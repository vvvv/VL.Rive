using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDX.Direct3D11;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Import;
using VL.Lang;
using VL.Lib.Animation;
using VL.Lib.Collections;
using VL.Rive;
using VL.Rive.Internal;
using VL.Rive.Interop;
using VL.Stride.Input;
using MessageSeverity = VL.Lang.MessageSeverity;
using PixelFormat = SharpDX.DXGI.Format;

namespace VL.Rive;

/// <summary>
/// Draws a scene (a state machine or an animation) of an artboard of a Rive file, binds a view model instance to it,
/// and passes pointer input on to it.
/// </summary>
/// <remarks>
/// The renderer makes an instance of the artboard of its own, so it has a state and a view model of its own, and
/// advances the scene once per frame. Renderers of the same file draw one after the other with the render context of
/// the file.
/// </remarks>
[ProcessNode(HasStateOutput = true, FragmentSelection = FragmentSelection.Explicit)]
[Smell(SymbolSmell.Advanced)]
public sealed partial class RiveRenderer : RendererBase
{
    readonly NodeContext nodeContext;
    readonly AppHost appHost;
    readonly ILogger logger;

    RiveRenderTargetD3D11? riveRenderTarget;

    RiveContext? riveContext;
    RiveArtboardInstance? riveArtboard;
    RiveScene? riveScene;
    RiveViewModel? riveViewModelInstance;
    RiveViewModel? riveDefaultViewModelInstance;
    IFrameClock frameClock;
    Int2 lastSize;
    RiveMat2D alignmentMat;

    readonly SerialDisposable inputSubscription = new SerialDisposable();
    readonly SerialDisposable viewModelWarning = new SerialDisposable();
    readonly SerialDisposable lookupWarning = new SerialDisposable();
    IInputSource? lastInputSource;

    string? artboardName;
    string? sceneName;
    RiveFit riveFit;
    RiveAlignment riveAlignment;
    Optional<RectangleF> riveFrame;
    Optional<RectangleF> riveContent;
    float riveScaleFactor;

    [Fragment]
    [Smell(SymbolSmell.Internal)]
    public RiveRenderer([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    {
        this.nodeContext = nodeContext;
        appHost = nodeContext.AppHost;
        logger = nodeContext.GetLogger();
        frameClock = appHost.Services.GetRequiredService<IFrameClock>();
    }

    /// <param name="file">The file to draw</param>
    /// <param name="artboardName">The artboard, the default one if empty. The editor offers the ones in the file.</param>
    /// <param name="sceneName">The state machine or animation, the default one if empty. The editor offers the ones of the artboard.</param>
    /// <param name="viewModel">The view model instance to bind. Another one replaces the first. With none, an instance of the view model of the artboard is bound, it comes out of the renderer.</param>
    [Fragment]
    [Smell(SymbolSmell.Internal)]
    public void Update(
        RiveContext? file,
        [ChoiceProvider(typeof(RiveArtboardChoiceProvider))] string? artboardName,
        [ChoiceProvider(typeof(RiveSceneChoiceProvider))] string? sceneName,
        RiveViewModel? viewModel,
        [Pin(Visibility = Model.PinVisibility.Optional)] RiveFit fit,
        [Pin(Visibility = Model.PinVisibility.Optional)] RiveAlignment alignment,
        [Pin(Visibility = Model.PinVisibility.Optional)] Optional<RectangleF> frame,
        [Pin(Visibility = Model.PinVisibility.Optional)] Optional<RectangleF> content,
        [Pin(Visibility = Model.PinVisibility.Optional)] [DefaultValue(1f)] float scaleFactor)
    {
        riveFit = fit;
        riveAlignment = alignment;
        riveFrame = frame;
        riveContent = content;
        riveScaleFactor = scaleFactor;

        // A file that is gone counts as none
        if (file is { IsDisposed: true })
            file = null;

        // Load file
        var newFile = !ReferenceEquals(file, riveContext);
        if (newFile)
        {
            // Before the new one is remembered: what was made from the old one is let go of there
            DisposeRiveResources();
            riveContext = file;
        }

        // Load artboard
        var newArtboard = newFile || artboardName != this.artboardName;
        if (newArtboard)
        {
            this.artboardName = artboardName;

            DisposeRiveContext();

            var riveFile = riveContext?.RiveFile;
            if (string.IsNullOrEmpty(artboardName))
                riveArtboard = riveFile?.GetArtboardDefault();
            else
            {
                riveArtboard = riveFile?.GetArtboard(artboardName);
                if (riveArtboard is null && riveContext != null)
                {
                    // Shown on the node until the file or the name changes
                    lookupWarning.Disposable = nodeContext.AddPersistentMessage(MessageSeverity.Warning,
                        $"Rive artboard '{artboardName}' not found in file '{riveContext.FilePath}'.");
                }
            }

            // The artboard goes away with the file
            if (riveArtboard != null)
                riveContext!.Track(riveArtboard);
        }

        // Bind view model instance
        if (riveArtboard != null)
        {
            // None, or a disposed one, means the one the artboard is made for
            if (viewModel != null && viewModel.IsDisposed)
                viewModel = null;
            if (viewModel is null)
            {
                riveDefaultViewModelInstance ??= riveContext!.ViewModels.ElementAtOrDefault(riveArtboard.ViewModelId)?.CreateDefaultInstance();
                viewModel = riveDefaultViewModelInstance;
            }

            // Bind when the instance changes: another one replaces the first. Only the artboard is bound, the state
            // machines made from it share its data context and an animation has nothing to bind.
            if (!ReferenceEquals(viewModel, riveViewModelInstance))
            {
                // Shown on the node for as long as this instance is bound
                viewModelWarning.Disposable = viewModel != null && viewModel.ViewModelId != riveArtboard.ViewModelId
                    ? nodeContext.AddPersistentMessage(MessageSeverity.Warning, MismatchMessage(
                        riveArtboard.Name,
                        riveContext!.ViewModels.ElementAtOrDefault(riveArtboard.ViewModelId)?.Name,
                        viewModel.ToString()))
                    : null;

                riveArtboard.BindViewModelInstance(viewModel);
                riveViewModelInstance = viewModel;
            }
        }

        // Load scene, after the artboard is bound: a state machine made from a bound artboard shares its data context
        if (newArtboard || sceneName != this.sceneName)
        {
            this.sceneName = sceneName;

            ReleaseAndSetNull(ref riveScene);
            if (riveArtboard != null)
                lookupWarning.Disposable = null;

            if (string.IsNullOrEmpty(sceneName))
                riveScene = riveArtboard?.GetDefaultScene();
            else
            {
                riveScene = riveArtboard?.GetScene(sceneName);
                if (riveScene is null && riveArtboard != null)
                {
                    lookupWarning.Disposable = nodeContext.AddPersistentMessage(MessageSeverity.Warning,
                        $"Rive scene '{sceneName}' not found in artboard '{riveArtboard.Name}' of file '{riveContext!.FilePath}'.");
                }
            }

            // The scene goes away with the file, before the artboard it is made from
            if (riveScene != null)
                riveContext!.Track(riveScene);
        }

        if (riveScene is null)
            return;

        riveScene.AdvanceAndApply((float)frameClock.TimeDifference);

        // Tells whoever listens what the scene changed in this frame: the bound instance, but also the items of its
        // lists and nested instances, which nobody else tells about
        riveContext!.AcknowledgeChanges();
    }

    /// <summary>The view model instance that is bound: the one that came in, or the default one of the artboard.</summary>
    [Fragment(IsDefault = true)]
    [Smell(SymbolSmell.Internal)]
    public RiveViewModel? ViewModel => riveViewModelInstance is { IsDisposed: false } ? riveViewModelInstance : null;

    public string DumpFileAsJson()
    {
        var riveFile = riveContext is { IsDisposed: false } ? riveContext.RiveFile : null;
        if (riveFile is null)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        riveFile.WriteRiveFileAsJson(sb);
        return sb.ToString();
    }

    public RectangleF GetArtboardBounds()
    {
        if (riveArtboard is null || riveArtboard.IsClosed)
            return RectangleF.Empty;
        var bounds = riveArtboard.Bounds;
        return new RectangleF(bounds.minX, bounds.minY, bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
    }

    protected override unsafe void DrawCore(RenderDrawContext context)
    {
        if (riveContext is null || riveContext.IsDisposed || riveScene is null || riveScene.IsClosed)
            return;

        var riveRenderContext = riveContext.RenderContext;
        var riveDeferredHost = riveContext.DeferredHost;

        // Subscribe to input events - in case we have many sinks we assume that there's only one input source active
        var inputSource = context.RenderContext.Tags.Get(InputExtensions.WindowInputSource);
        if (inputSource != lastInputSource)
        {
            lastInputSource = inputSource;
            inputSubscription.Disposable = SubscribeToInputSource(inputSource, context);
        }

        var renderTarget = context.CommandList.RenderTarget;
        if (renderTarget is null)
            return;

        var nativeRenderTarget = SharpDXInterop.GetNativeResource(renderTarget) as Texture2D;
        if (nativeRenderTarget is null)
            return;

        if (!IsSupportedByRive(nativeRenderTarget.Description.Format))
        {
            logger.LogError($"The render target format '{renderTarget.Format}' is not supported by Rive. In case you render to a texture set its format to RGBA_Typeless and its view to RGBA_Srgb.");
            return;
        }


        var frameDescriptor = new FrameDescriptor
        {
            RenderTargetWidth = (uint)renderTarget.Width,
            RenderTargetHeight = (uint)renderTarget.Height,
            LoadAction = LoadAction.PreserveRenderTarget,
        };
        // Deferred: record the scene, replay it at the end. Otherwise draw straight into the open frame.
        var renderer = riveDeferredHost?.BeginFrame(renderTarget.Width, renderTarget.Height);
        if (renderer is null)
        {
            riveRenderContext.BeginFrame(in frameDescriptor);
            renderer = riveContext.ImmediateRenderer;
        }

        alignmentMat = Methods.rive_ComputeAlignment(
            riveFit.ToNative(),
            riveAlignment.ToNative(),
            frame: riveFrame.ToNative(new RiveAABB(0, 0, renderTarget.Width, renderTarget.Height)),
            content: riveContent.ToNative(riveScene.Bounds), 
            scaleFactor: riveScaleFactor);

        renderer.Save();
        renderer.Transform(in alignmentMat);
        riveScene.Draw(renderer);
        renderer.Restore();

        var size = new Int2(renderTarget.Width, renderTarget.Height);
        if (riveRenderTarget is null || lastSize != size)
        {
            lastSize = size;
            ReleaseAndSetNull(ref riveRenderTarget);
            riveRenderTarget = riveContext.MakeRenderTarget(size.X, size.Y);
        }
        riveRenderTarget.SetTargetTexture(nativeRenderTarget.NativePointer);

        if (riveDeferredHost != null)
        {
            var (frame, safeFrame) = riveRenderContext.NextFrame();
            riveDeferredHost.EndFrame(riveRenderTarget, frame, safeFrame);
        }
        else
        {
            riveRenderContext.Flush(riveRenderTarget);
        }

        // Release render target texture
        riveRenderTarget.SetTargetTexture(default);

        // Restore Stride's pipeline state
        // TODO: Turn this into an official API in Stride
        context.CommandList.RestorePipelineState();

        // See submodules\rive-runtime\renderer\src\d3d11\render_context_d3d_impl.cpp
        static bool IsSupportedByRive(PixelFormat format)
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
    }

    protected override void Destroy()
    {
        inputSubscription.Dispose();
        viewModelWarning.Dispose();
        lookupWarning.Dispose();

        DisposeRiveResources();

        base.Destroy();
    }

    private void DisposeRiveResources()
    {
        DisposeRiveContext();
        DisposeRiveRenderResources();
    }

    private void DisposeRiveContext()
    {
        viewModelWarning.Disposable = null;
        lookupWarning.Disposable = null;
        riveViewModelInstance = null;
        DisposeAndSetNull(ref riveDefaultViewModelInstance);
        ReleaseAndSetNull(ref riveScene); // before the artboard it is made from
        ReleaseAndSetNull(ref riveArtboard);
    }

    private void DisposeRiveRenderResources()
    {
        ReleaseAndSetNull(ref riveRenderTarget);
    }

    static void DisposeAndSetNull<T>(ref T? resource) where T : class, IDisposable => Interlocked.Exchange(ref resource, null)?.Dispose();

    // Made through the file, which disposes it when it goes away: the file may be gone by now, and then the resource with it
    void ReleaseAndSetNull<T>(ref T? resource) where T : class, IDisposable
    {
        if (Interlocked.Exchange(ref resource, null) is { } released)
        {
            released.Dispose();
            riveContext?.Untrack(released);
        }
    }

    /// <summary>Tells which view model the artboard is made for (null if it has none) and which one the instance is of.</summary>
    internal static string MismatchMessage(string artboardName, string? expectedViewModel, string actualViewModel)
        => expectedViewModel is null
            ? $"The artboard '{artboardName}' has no view model, so it will not show the values of the view model instance '{actualViewModel}'."
            : $"The artboard '{artboardName}' shows an instance of the view model '{expectedViewModel}', but the instance is of the view model '{actualViewModel}', so the artboard will not show its values.";
}