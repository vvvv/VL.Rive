using Microsoft.Extensions.Logging;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Vector2 = System.Numerics.Vector2;
using System.Reactive.Disposables;
using VL.Core;
using VL.Lib.Collections;
using VL.Rive.Interop;
using VL.Stride.Input;
using SharpDX.Direct3D11;
using PixelFormat = SharpDX.DXGI.Format;

namespace VL.Rive;

/// <summary>
/// Draws a scene into the render target of the current command list and passes pointer input on to it. The part of
/// the renderer nodes that does not depend on how they get their scene.
/// </summary>
internal sealed class RiveSceneDrawer : IDisposable
{
    private readonly ILogger logger;
    private readonly SerialDisposable inputSubscription = new();
    private IInputSource? lastInputSource;
    private RiveRenderTargetD3D11? renderTarget;
    private RiveFile? renderTargetFile;
    private Int2 lastSize;
    private RiveMat2D alignmentMat;

    public RiveSceneDrawer(ILogger logger)
    {
        this.logger = logger;
    }

    public RiveScene? Scene { get; set; }
    public RiveFit Fit { get; set; }
    public RiveAlignment Alignment { get; set; }
    public Optional<RectangleF> Frame { get; set; }
    public Optional<RectangleF> Content { get; set; }
    public float ScaleFactor { get; set; } = 1f;

    public unsafe void Draw(RenderDrawContext context)
    {
        var sceneHandle = Scene;
        if (sceneHandle is null || sceneHandle.IsDisposed)
            return;
        var fileHandle = sceneHandle.Artboard.FileHandle;
        if (fileHandle.IsDisposed)
            return;
        var scene = sceneHandle.Scene;
        var riveRenderContext = fileHandle.RenderContext;

        // Subscribe to input events - in case we have many sinks we assume that there's only one input source active
        var inputSource = context.RenderContext.Tags.Get(InputExtensions.WindowInputSource);
        if (inputSource != lastInputSource)
        {
            lastInputSource = inputSource;
            inputSubscription.Disposable = SubscribeToInputSource(inputSource, context);
        }

        var commandListTarget = context.CommandList.RenderTarget;
        if (commandListTarget is null)
            return;

        var nativeRenderTarget = SharpDXInterop.GetNativeResource(commandListTarget) as Texture2D;
        if (nativeRenderTarget is null)
            return;

        if (!IsSupportedByRive(nativeRenderTarget.Description.Format))
        {
            logger.LogError($"The render target format '{commandListTarget.Format}' is not supported by Rive. In case you render to a texture set its format to RGBA_Typeless and its view to RGBA_Srgb.");
            return;
        }

        var frameDescriptor = new FrameDescriptor
        {
            RenderTargetWidth = (uint)commandListTarget.Width,
            RenderTargetHeight = (uint)commandListTarget.Height,
            LoadAction = LoadAction.PreserveRenderTarget,
        };

        // Deferred: record the scene, replay it at the end. Otherwise draw straight into the open frame.
        // Renderers of the same file come one after the other here, each with a frame of its own.
        var deferredHost = fileHandle.DeferredHost;
        var renderer = deferredHost?.BeginFrame(commandListTarget.Width, commandListTarget.Height);
        if (renderer is null)
        {
            riveRenderContext.BeginFrame(in frameDescriptor);
            renderer = fileHandle.ImmediateRenderer;
        }

        alignmentMat = Methods.rive_ComputeAlignment(
            Fit.ToNative(),
            Alignment.ToNative(),
            frame: Frame.ToNative(new RiveAABB(0, 0, commandListTarget.Width, commandListTarget.Height)),
            content: Content.ToNative(scene.Bounds),
            scaleFactor: ScaleFactor);

        renderer.Save();
        renderer.Transform(in alignmentMat);
        scene.Draw(renderer);
        renderer.Restore();

        var size = new Int2(commandListTarget.Width, commandListTarget.Height);
        if (renderTarget is null || renderTarget.IsClosed || !ReferenceEquals(renderTargetFile, fileHandle) || lastSize != size)
        {
            lastSize = size;
            DisposeRenderTarget();
            renderTarget = fileHandle.MakeRenderTarget(size.X, size.Y);
            renderTargetFile = fileHandle;
        }
        renderTarget.SetTargetTexture(nativeRenderTarget.NativePointer);

        if (deferredHost != null)
        {
            var (frame, safeFrame) = riveRenderContext.NextFrame();
            deferredHost.EndFrame(renderTarget, frame, safeFrame);
        }
        else
        {
            riveRenderContext.Flush(renderTarget);
        }

        // Release render target texture
        renderTarget.SetTargetTexture(default);

        // Restore Stride's pipeline state
        // TODO: Turn this into an official API in Stride
        context.CommandList.RestorePipelineState();
    }

    // See submodules\rive-runtime\renderer\src\d3d11\render_context_d3d_impl.cpp
    private static bool IsSupportedByRive(PixelFormat format)
    {
        switch (format)
        {
            case PixelFormat.B8G8R8A8_UNorm:
            case PixelFormat.B8G8R8A8_Typeless:
            case PixelFormat.R8G8B8A8_UNorm:
            case PixelFormat.R8G8B8A8_Typeless:
                return true;
        }
        return false;
    }

    private void DisposeRenderTarget()
    {
        var target = Interlocked.Exchange(ref renderTarget, null);
        if (target != null)
        {
            // The file may be gone by now, and then the target with it
            target.Dispose();
            renderTargetFile?.Untrack(target);
        }
        renderTargetFile = null;
    }

    public void Dispose()
    {
        inputSubscription.Dispose();
        DisposeRenderTarget();
    }

    private IDisposable SubscribeToInputSource(IInputSource? inputSource, RenderDrawContext context)
    {
        if (inputSource is null)
            return Disposable.Empty;

        var inputManager = context.RenderContext.Services.GetService<InputManager>();
        if (inputManager is null)
            return Disposable.Empty;

        var pointerListener = NewPointerListener(inputSource);
        inputManager.AddListener(pointerListener);

        return Disposable.Create(() => inputManager.RemoveListener(pointerListener));
    }

    private IInputEventListener NewPointerListener(IInputSource inputSource)
    {
        return new AnonymousEventListener<PointerEvent>(e =>
        {
            if (e.Device.Source != inputSource)
                return;

            var sceneHandle = Scene;
            if (sceneHandle is null || sceneHandle.IsDisposed)
                return;
            var scene = sceneHandle.Scene;

            var inverseViewTrasform = alignmentMat.InvertOrIdentity();
            var position = inverseViewTrasform * new Vector2(e.AbsolutePosition.X, e.AbsolutePosition.Y);

            switch (e.EventType)
            {
                case PointerEventType.Pressed:
                    scene.PointerDown(position.X, position.Y, e.PointerId);
                    break;
                case PointerEventType.Moved:
                    scene.PointerMove(position.X, position.Y, 0f, e.PointerId);
                    break;
                case PointerEventType.Released:
                    scene.PointerUp(position.X, position.Y, e.PointerId);
                    break;
                case PointerEventType.Canceled:
                    scene.PointerExit(position.X, position.Y, e.PointerId);
                    break;
                default:
                    break;
            }
        });
    }

    private sealed class AnonymousEventListener<T> : IInputEventListener<T>
        where T : InputEvent
    {
        private readonly Action<T> processEventAction;

        public AnonymousEventListener(Action<T> processEventAction)
        {
            this.processEventAction = processEventAction;
        }

        public void ProcessEvent(T inputEvent) => processEventAction(inputEvent);
    }
}
