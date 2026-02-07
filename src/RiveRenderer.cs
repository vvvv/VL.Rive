using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDX.Direct3D11;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Animation;
using VL.Lib.Collections;
using VL.Rive;
using VL.Rive.Internal;
using VL.Rive.Interop;
using VL.Stride.Input;
using Path = VL.Lib.IO.Path;
using PixelFormat = SharpDX.DXGI.Format;

namespace VL.Rive;

[ProcessNode(HasStateOutput = true, FragmentSelection = FragmentSelection.Explicit)]
[Smell(SymbolSmell.Advanced)]
public sealed partial class RiveRenderer : RendererBase
{
    readonly NodeContext nodeContext;
    readonly AppHost appHost;
    readonly ILogger logger;

    RiveRenderContextD3D11? riveRenderContext;
    RiveRenderTargetD3D11? riveRenderTarget;

    Interop.RiveRenderer? riveRenderer;
    RiveContext? riveContext;
    Path? riveFilePath;
    RiveArtboardInstance? riveArtboard;
    RiveScene? riveScene;
    RiveViewModel? riveViewModelInstance;
    IFrameClock frameClock;
    readonly IGraphicsDeviceService graphicsDeviceService;
    Int2 lastSize;
    RiveMat2D alignmentMat;

    readonly SerialDisposable inputSubscription = new SerialDisposable();
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
        graphicsDeviceService = appHost.Services.GetRequiredService<Game>().Services.GetService<IGraphicsDeviceService>();
    }

    [Fragment]
    [Smell(SymbolSmell.Internal)]
    public void Update(
        Path? file, 
        string? artboardName, 
        string? sceneName, 
        [Pin(Visibility = Model.PinVisibility.Optional)] RiveFit fit,
        [Pin(Visibility = Model.PinVisibility.Optional)] RiveAlignment alignment,
        [Pin(Visibility = Model.PinVisibility.Optional)] Optional<RectangleF> frame,
        [Pin(Visibility = Model.PinVisibility.Optional)] Optional<RectangleF> content,
        [Pin(Visibility = Model.PinVisibility.Optional)] [DefaultValue(1f)] float scaleFactor, 
        bool reload)
    {
        riveFit = fit;
        riveAlignment = alignment;
        riveFrame = frame;
        riveContent = content;
        riveScaleFactor = scaleFactor;

        // Native device can change - check on each update
        var nativeDevice = SharpDXInterop.GetNativeDevice(graphicsDeviceService.GraphicsDevice) as Device;
        if (riveRenderContext?.DevicePointer != nativeDevice?.NativePointer)
        {
            DisposeRiveResources();

            if (nativeDevice != null)
            {
                riveRenderContext = RiveRenderContextD3D11.Create(nativeDevice.NativePointer, nativeDevice.ImmediateContext.NativePointer);
                riveRenderer = riveRenderContext.CreateRenderer();
            }
        }

        // Load file
        if (reload || file != riveFilePath)
        {
            riveFilePath = file;

            DisposeRiveContext();

            var filePath = file?.ToString();
            if (!string.IsNullOrEmpty(filePath))
            {
                var riveFile = riveRenderContext?.LoadFile(file);
                if (riveFile != null)
                    riveContext = new RiveContext(nodeContext, riveFile);
            }
        }

        // Load artboard and view model instance
        if (riveArtboard is null || artboardName != this.artboardName)
        {
            this.artboardName = artboardName;

            DisposeAndSetNull(ref riveArtboard);
            DisposeAndSetNull(ref riveScene);
            riveViewModelInstance = null;

            var riveFile = riveContext?.RiveFile;
            if (string.IsNullOrEmpty(artboardName))
                riveArtboard = riveFile?.GetArtboardDefault();
            else
            {
                riveArtboard = riveFile?.GetArtboard(artboardName);
                if (riveArtboard is null)
                    throw new ArgumentException($"Rive artboard '{artboardName}' not found in file '{file}'.");
            }

            if (riveArtboard != null)
            {
                var viewModelRuntime = riveContext!.ViewModels.ElementAtOrDefault(riveArtboard.ViewModelId);
                riveViewModelInstance = viewModelRuntime?.CreateDefaultInstance();
            }
        }

        // Load scene
        if (riveScene is null || sceneName != this.sceneName)
        {
            this.sceneName = sceneName;

            DisposeAndSetNull(ref riveScene);

            if (string.IsNullOrEmpty(sceneName))
                riveScene = riveArtboard?.GetDefaultScene();
            else
            {
                riveScene = riveArtboard?.GetScene(sceneName);
                if (riveScene is null)
                    throw new ArgumentException($"Rive scene '{sceneName}' not found in artboard '{artboardName}' of file '{file}'.");
            }

            if (riveViewModelInstance != null)
                riveScene?.BindViewModelInstance(riveViewModelInstance);
        }

        if (riveScene is null)
            return;

        riveScene.AdvanceAndApply((float)frameClock.TimeDifference);

        riveViewModelInstance?.AcknowledgeChanges();
    }

    [Fragment(IsDefault = true)]
    [Smell(SymbolSmell.Internal)]
    public RiveViewModel? ViewModel => riveViewModelInstance;

    public string DumpFileAsJson()
    {
        var riveFile = riveContext?.RiveFile;
        if (riveFile is null)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        riveFile.WriteRiveFileAsJson(sb);
        return sb.ToString();
    }

    public RectangleF GetArtboardBounds()
    {
        if (riveArtboard is null)
            return RectangleF.Empty;
        var bounds = riveArtboard.Bounds;
        return new RectangleF(bounds.minX, bounds.minY, bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
    }

    protected override unsafe void DrawCore(RenderDrawContext context)
    {
        if (riveRenderContext is null || riveScene is null)
            return;

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
        riveRenderContext.BeginFrame(in frameDescriptor);

        alignmentMat = Methods.rive_ComputeAlignment(
            riveFit.ToNative(),
            riveAlignment.ToNative(),
            frame: riveFrame.ToNative(new RiveAABB(0, 0, renderTarget.Width, renderTarget.Height)),
            content: riveContent.ToNative(riveScene.Bounds), 
            scaleFactor: riveScaleFactor);

        riveRenderer!.Save();
        riveRenderer.Transform(in alignmentMat);
        riveScene.Draw(riveRenderer);
        riveRenderer.Restore();

        var size = new Int2(renderTarget.Width, renderTarget.Height);
        if (riveRenderTarget is null || lastSize != size)
        {
            lastSize = size;
            DisposeAndSetNull(ref riveRenderTarget);
            riveRenderTarget = riveRenderContext.MakeRenderTarget(size.X, size.Y);
        }
        riveRenderTarget.SetTargetTexture(nativeRenderTarget.NativePointer);

        riveRenderContext.Flush(riveRenderTarget);

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
        DisposeAndSetNull(ref riveScene);
        DisposeAndSetNull(ref riveArtboard);
        DisposeAndSetNull(ref riveContext);
    }

    private void DisposeRiveRenderResources()
    {
        DisposeAndSetNull(ref riveRenderer);
        DisposeAndSetNull(ref riveRenderTarget);
        DisposeAndSetNull(ref riveRenderContext);
    }

    static void DisposeAndSetNull<T>(ref T? resource) where T : class, IDisposable => Interlocked.Exchange(ref resource, null)?.Dispose();
}
