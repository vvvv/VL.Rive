using Microsoft.Extensions.DependencyInjection;
using VL.Rive.Interop;
using SharpDX.Direct3D11;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using System.Reactive.Disposables;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Animation;
using VL.Stride.Input;
using Path = VL.Lib.IO.Path;

namespace VL.Rive;

[ProcessNode(HasStateOutput = true)]
public sealed partial class RivePlayer : RendererBase
{
    Path? file;
    RiveRenderContextD3D11? riveRenderContext;
    RiveRenderTargetD3D11? riveRenderTarget;

    RiveRenderer? riveRenderer;
    RiveFile? riveFile;
    RiveArtboard? riveArtboard;
    RiveScene? riveScene;
    RiveViewModelInstance? riveViewModelInstance;
    bool needsReload;
    IFrameClock frameClock;
    Int2 lastSize;
    RiveMat2D alignmentMat;

    readonly SerialDisposable inputSubscription = new SerialDisposable();
    IInputSource? lastInputSource;

    public RivePlayer([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    {
        frameClock = nodeContext.AppHost.Services.GetRequiredService<IFrameClock>();
    }

    public void Update(Path? file)
    {
        if (file != this.file)
        {
            this.file = file;
            needsReload = true;
        }
    }

    protected override unsafe void DrawCore(RenderDrawContext context)
    {
        var graphicsDevice = context.GraphicsDevice;
        if (riveRenderContext is null)
            riveRenderContext = CreateRiveRenderContext(graphicsDevice);
        if (riveRenderContext is null)
            return;

        // Subscribe to input events - in case we have many sinks we assume that there's only one input source active
        var inputSource = context.RenderContext.Tags.Get(InputExtensions.WindowInputSource);
        if (inputSource != lastInputSource)
        {
            lastInputSource = inputSource;
            inputSubscription.Disposable = SubscribeToInputSource(inputSource, context);
        }

        var renderTarget = context.CommandList.RenderTarget;

        var size = new Int2(renderTarget.Width, renderTarget.Height);
        if (riveRenderer is null || lastSize != size)
        {
            lastSize = size;

            riveRenderer?.Dispose();
            riveRenderTarget?.Dispose();

            riveRenderer = riveRenderContext.CreateRenderer();
            riveRenderTarget = riveRenderContext.MakeRenderTarget(size.X, size.Y);
        }

        if (needsReload)
        {
            needsReload = false;

            riveViewModelInstance?.Dispose();
            riveScene?.Dispose();
            riveArtboard?.Dispose();
            riveFile?.Dispose();

            if (file != null)
                riveFile = riveRenderContext.LoadFile(file);
            else
                riveFile = null;

            if (riveFile != null)
            {
                riveArtboard = riveFile.GetArtboardDefault();
                riveScene = riveArtboard.DefaultScene();
                // Needs more careful memory management
                //riveViewModelInstance = riveFile.DefaultArtboardViewModel(riveArtboard);
                //if (riveViewModelInstance != default)
                //{
                //    var proprs = riveViewModelInstance.Properties;
                //}
            }
        }

        if (riveScene is null || riveRenderTarget is null)
            return;

        riveScene.AdvanceAndApply((float)frameClock.TimeDifference);

        var frameDescriptor = new FrameDescriptor
        {
            RenderTargetWidth = (uint)renderTarget.Width,
            RenderTargetHeight = (uint)renderTarget.Height,
            MsaaSampleCount = renderTarget.MultisampleCount != MultisampleCount.None ? (int)renderTarget.MultisampleCount : 0,
        };
        riveRenderContext.BeginFrame(in frameDescriptor);

        alignmentMat = Methods.rive_ComputeAlignment(RiveFit.Contain, RiveAlignment.center, new RiveAABB(0, 0, renderTarget.Width, renderTarget.Height), riveScene.Bounds, 1f);

        riveRenderer.Save();
        riveRenderer.Transform(in alignmentMat);
        riveScene.Draw(riveRenderer);
        riveRenderer.Restore();

        var nativeRenderTarget = SharpDXInterop.GetNativeResource(renderTarget) as Texture2D;
        if (nativeRenderTarget is null)
            return;
        riveRenderTarget.SetTargetTexture(nativeRenderTarget.NativePointer);
        riveRenderContext.Flush(riveRenderTarget);

        // Release render target texture
        riveRenderTarget.SetTargetTexture(default);
    }

    protected override void Destroy()
    {
        inputSubscription.Dispose();

        riveScene?.Dispose();
        riveArtboard?.Dispose();
        riveFile?.Dispose();
        riveRenderer?.Dispose();
        riveRenderTarget?.Dispose();
        riveRenderContext?.Dispose();

        base.Destroy();
    }

    private RiveRenderContextD3D11? CreateRiveRenderContext(GraphicsDevice device)
    {
        var nativeDevice = SharpDXInterop.GetNativeDevice(device) as Device;
        var nativeContext = SharpDXInterop.GetNativeDeviceContext(device) as DeviceContext;
        if (nativeDevice is null || nativeContext is null)
            return default;

        return RiveRenderContextD3D11.Create(nativeDevice.NativePointer, nativeContext.NativePointer);
    }
}
