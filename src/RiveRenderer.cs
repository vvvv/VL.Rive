using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
using VL.Lib.Basics.Video;
using VL.Lib.Collections;
using VL.Rive;
using VL.Rive.Internal;
using VL.Rive.Interop;
using VL.Stride.Graphics;
using VL.Stride.Input;
using MessageSeverity = VL.Lang.MessageSeverity;
using PixelFormat = Stride.Graphics.PixelFormat;

namespace VL.Rive;

/// <summary>
/// Draws a scene (a state machine or an animation) of an artboard of a Rive file, binds a view model instance to it,
/// and passes pointer input on to it.
/// </summary>
/// <remarks>
/// The renderer makes an instance of the artboard of its own, so it has a state of its own. It binds the view model
/// instance that comes in, or an instance of the view model of the artboard if there is none.
/// The scene is advanced once per frame, unless Progress drives an animation by hand or the renderer is not enabled.
/// With Fit set to Layout the artboard is resized to the frame.
/// Renderers of the same file draw one after the other with the render context of the file.
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
    Vector2? layoutSize; // the size the artboard was resized to for Fit.Layout, null while it has its own

    readonly SerialDisposable inputSubscription = new SerialDisposable();
    readonly SerialDisposable viewModelWarning = new SerialDisposable();
    readonly SerialDisposable lookupWarning = new SerialDisposable();
    readonly SerialDisposable seekWarning = new SerialDisposable();
    IInputSource? lastInputSource;

    string? artboardName;
    string? sceneName;
    RiveFit riveFit;
    RiveAlignment riveAlignment;
    Optional<RectangleF> riveFrame;
    Optional<RectangleF> riveContent;
    float riveScaleFactor;
    bool riveEnabled = true;

    [Fragment]
    [Smell(SymbolSmell.Internal)]
    public RiveRenderer([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    {
        this.nodeContext = nodeContext;
        appHost = nodeContext.AppHost;
        logger = nodeContext.GetLogger();
        frameClock = appHost.Services.GetRequiredService<IFrameClock>();
    }

    /// <param name="input">The file to draw</param>
    /// <param name="artboard">The artboard, the default one if empty. The editor offers the ones in the file.</param>
    /// <param name="scene">The state machine or animation, the default one if empty. The editor offers the ones of the artboard.</param>
    /// <param name="fit">How the artboard is fitted into the frame</param>
    /// <param name="alignment">Applies to all Fit modes except: Layout and Fill</param>
    /// <param name="frame">The area to draw into, the whole render target if not set</param>
    /// <param name="content">The part of the artboard to show, all of it if not set</param>
    /// <param name="scaleFactor">Only works in Layout Fit mode</param>
    /// <param name="enabled">Draws the scene. If false nothing is rendered and the scene is not advanced.</param>
    /// <param name="progress">Progress for a linear animation from 0 to 1. While set, the animation does not advance by itself. A state machine has no time to scrub, it keeps advancing.</param>
    /// <param name="viewModel">The view model instance to bind. Another one replaces the first. With none, an instance of the view model of the artboard is bound, it comes out of the renderer.</param>
    [Fragment]
    [Smell(SymbolSmell.Internal)]
    public void Update(
        RiveContext? input,
        [ChoiceProvider(typeof(RiveArtboardChoiceProvider))] string? artboard,
        [ChoiceProvider(typeof(RiveSceneChoiceProvider))] string? scene,
        RiveViewModel? viewModel,
        RiveFit fit,
        [Pin(Visibility = Model.PinVisibility.Optional)] RiveAlignment alignment,
        [Pin(Visibility = Model.PinVisibility.Hidden)] Optional<RectangleF> frame,
        [Pin(Visibility = Model.PinVisibility.Hidden)] Optional<RectangleF> content,
        [Pin(Visibility = Model.PinVisibility.Optional)] [DefaultValue(1f)] float scaleFactor,
        Optional<float> progress,
        [DefaultValue(true)] bool enabled)
    {
        riveFit = fit;
        riveAlignment = alignment;
        riveFrame = frame;
        riveContent = content;
        riveScaleFactor = scaleFactor;
        riveEnabled = enabled;

        // A file that is gone counts as none
        if (input is { IsDisposed: true })
            input = null;

        // Load file
        var newFile = !ReferenceEquals(input, riveContext);
        if (newFile)
        {
            // Before the new one is remembered: what was made from the old one is let go of there
            DisposeRiveResources();
            riveContext = input;
        }

        // Load artboard
        var newArtboard = newFile || artboard != this.artboardName;
        if (newArtboard)
        {
            this.artboardName = artboard;

            DisposeRiveContext();

            var riveFile = riveContext?.RiveFile;
            if (string.IsNullOrEmpty(artboard))
                riveArtboard = riveFile?.GetArtboardDefault();
            else
            {
                riveArtboard = riveFile?.GetArtboard(artboard);
                if (riveArtboard is null && riveContext != null)
                {
                    // Shown on the node until the file or the name changes
                    lookupWarning.Disposable = nodeContext.AddPersistentMessage(MessageSeverity.Warning,
                        $"Rive artboard '{artboard}' not found in file '{riveContext.FilePath}'.");
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
        if (newArtboard || scene != this.sceneName)
        {
            this.sceneName = scene;

            ReleaseAndSetNull(ref riveScene);
            if (riveArtboard != null)
                lookupWarning.Disposable = null;

            if (string.IsNullOrEmpty(scene))
                riveScene = riveArtboard?.GetDefaultScene();
            else
            {
                riveScene = riveArtboard?.GetScene(scene);
                if (riveScene is null && riveArtboard != null)
                {
                    lookupWarning.Disposable = nodeContext.AddPersistentMessage(MessageSeverity.Warning,
                        $"Rive scene '{scene}' not found in artboard '{riveArtboard.Name}' of file '{riveContext!.FilePath}'.");
                }
            }

            // The scene goes away with the file, before the artboard it is made from
            if (riveScene != null)
                riveContext!.Track(riveScene);
        }

        if (riveScene is null || !enabled)
            return;

        if (progress.HasValue && riveScene.SeekNormalized(progress.Value))
        {
            seekWarning.Disposable = null;
        }
        else
        {
            if (progress.HasValue && seekWarning.Disposable is null)
            {
                seekWarning.Disposable = nodeContext.AddPersistentMessage(MessageSeverity.Warning,
                    $"Rive scene '{riveScene.Name}' is a state machine, it can't be scrubbed with Progress.");
            }
            else if (!progress.HasValue)
                seekWarning.Disposable = null;

            riveScene.AdvanceAndApply((float)frameClock.TimeDifference);
        }

        // Tells whoever listens what the scene changed in this frame: the bound instance, but also the items of its
        // lists and nested instances, which nobody else tells about
        riveContext!.AcknowledgeChanges();
    }

    /// <summary>The view model instance that is bound: the one that came in, or the default one of the artboard.</summary>
    [Fragment(IsDefault = true)]
    [Smell(SymbolSmell.Hidden)]
    public RiveViewModel? ViewModel => riveViewModelInstance is { IsDisposed: false } ? riveViewModelInstance : null;

    [Smell(SymbolSmell.Internal)]
    public RectangleF GetArtboardBounds()
    {
        if (riveArtboard is null || riveArtboard.IsClosed)
            return RectangleF.Empty;
        var bounds = riveArtboard.Bounds;
        return new RectangleF(bounds.minX, bounds.minY, bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
    }

    protected override unsafe void DrawCore(RenderDrawContext context)
    {
        if (!riveEnabled || riveContext is null || riveContext.IsDisposed || riveScene is null || riveScene.IsClosed)
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

        if (!renderTarget.TryGetNativeResourcePointer(GraphicsDeviceType.Direct3D11, out var nativeRenderTarget))
            return;

        if (!IsSupportedByRive(renderTarget.Description.Format))
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

        var riveFrameAabb = riveFrame.ToNative(new RiveAABB(0, 0, renderTarget.Width, renderTarget.Height));
        ResizeArtboard(riveFrameAabb);

        alignmentMat = Methods.rive_ComputeAlignment(
            riveFit.ToNative(),
            riveAlignment.ToNative(),
            frame: riveFrameAabb,
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
        riveRenderTarget.SetTargetTexture(nativeRenderTarget);

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
        context.CommandList.RestoreCommandListState();

        // See submodules\rive-runtime\renderer\src\d3d11\render_context_d3d_impl.cpp
        static bool IsSupportedByRive(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.B8G8R8A8_UNorm:
                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                case PixelFormat.B8G8R8A8_Typeless:
                case PixelFormat.R8G8B8A8_UNorm:
                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                case PixelFormat.R8G8B8A8_Typeless:
                    return true;
            }
            return false;
        }
    }

    // In Layout mode the artboard is resized to the frame (in units of the scale factor), so its layout fills it:
    // https://rive.app/docs/runtimes/cpp/rendering-loop. Every other mode shows it at the size it has in the file.
    private void ResizeArtboard(RiveAABB frame)
    {
        if (riveArtboard is null || riveArtboard.IsClosed || riveScene is null)
            return;

        if (riveFit == RiveFit.Layout)
        {
            var scale = riveScaleFactor > 0f ? riveScaleFactor : 1f;
            var size = new Vector2((frame.maxX - frame.minX) / scale, (frame.maxY - frame.minY) / scale);
            if (layoutSize != size)
            {
                layoutSize = size;
                riveArtboard.SetSize(size.X, size.Y);
                // Lays the content out again: the scene has to be advanced after a resize
                riveScene.AdvanceAndApply(0f);
            }
        }
        else if (layoutSize != null)
        {
            layoutSize = null;
            riveArtboard.ResetSize();
            riveScene.AdvanceAndApply(0f);
        }
    }

    protected override void Destroy()
    {
        inputSubscription.Dispose();
        viewModelWarning.Dispose();
        lookupWarning.Dispose();
        seekWarning.Dispose();

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
        layoutSize = null;
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