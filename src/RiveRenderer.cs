using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDX.Direct3D11;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Animation;
using VL.Lib.Reactive;
using VL.Rive;
using VL.Rive.Interop;
using VL.Stride.Input;
using Path = VL.Lib.IO.Path;

namespace VL.Rive;

[ProcessNode(HasStateOutput = true, FragmentSelection = FragmentSelection.Explicit)]
public sealed partial class RiveRenderer : RendererBase
{
    readonly ILogger logger;

    RiveRenderContextD3D11? riveRenderContext;
    RiveRenderTargetD3D11? riveRenderTarget;

    Interop.RiveRenderer? riveRenderer;
    RiveFile? riveFile;
    Path? riveFilePath;
    RiveArtboardInstance? riveArtboard;
    RiveScene? riveScene;
    RiveViewModelInstance? riveViewModelInstance;
    IFrameClock frameClock;
    readonly IGraphicsDeviceService graphicsDeviceService;
    Int2 lastSize;
    RiveMat2D alignmentMat;

    readonly SerialDisposable inputSubscription = new SerialDisposable();
    IInputSource? lastInputSource;

    readonly SerialDisposable viewModelSubscription = new SerialDisposable();
    object? lastViewModel;
    int needToWrite;

    string? artboardName;
    string? sceneName;
    RiveFit riveFit;
    RiveAlignment riveAlignment;
    Optional<RectangleF> riveFrame;
    Optional<RectangleF> riveContent;
    float riveScaleFactor;

    [Fragment]
    public RiveRenderer([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    {
        var appHost = nodeContext.AppHost;
        logger = nodeContext.GetLogger();
        frameClock = appHost.Services.GetRequiredService<IFrameClock>();
        graphicsDeviceService = appHost.Services.GetRequiredService<Game>().Services.GetService<IGraphicsDeviceService>();
    }

    [Fragment]
    public void Update(Path? file, string? artboardName, string? sceneName, RiveFit fit, RiveAlignment alignment, Optional<RectangleF> frame, Optional<RectangleF> content, [DefaultValue(1f)] float scaleFactor, object? viewModel, bool reload)
    {
        riveFit = fit;
        riveAlignment = alignment;
        riveFrame = frame;
        riveContent = content;
        riveScaleFactor = scaleFactor;

        if (viewModel != lastViewModel)
        {
            lastViewModel = viewModel;
            Interlocked.Increment(ref needToWrite);
            viewModelSubscription.Disposable = null;
            if (viewModel is IChannel c)
                viewModelSubscription.Disposable = c.ChannelOfObject.Subscribe(_ => Interlocked.Increment(ref needToWrite));
        }
        if (viewModel is IVLObject obj && !obj.Type.IsImmutable)
            Interlocked.Increment(ref needToWrite); // Force writing to Rive if the object is mutable

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

            DisposeRiveFileResources();

            riveFile = riveRenderContext?.LoadFile(file);
        }

        // Load artboard and view model instance
        if (riveArtboard is null || artboardName != this.artboardName)
        {
            this.artboardName = artboardName;

            DisposeAndSetNull(ref riveArtboard);
            DisposeAndSetNull(ref riveScene);
            riveViewModelInstance = null;

            if (string.IsNullOrEmpty(artboardName))
                riveArtboard = riveFile?.GetArtboardDefault();
            else
            {
                riveArtboard = riveFile?.GetArtboard(artboardName);
                if (riveArtboard is null)
                    throw new ArgumentException($"Rive artboard '{artboardName}' not found in file '{file}'.");
            }

            if (riveArtboard != null)
                riveViewModelInstance = riveFile?.DefaultArtboardViewModel(riveArtboard);
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

        // Write values to rive
        if (riveViewModelInstance != null && Interlocked.Exchange(ref needToWrite, 0) > 0)
            WriteValuesToRive(riveViewModelInstance, viewModel);

        riveScene.AdvanceAndApply((float)frameClock.TimeDifference);

        if (riveViewModelInstance != null)
            ReadValuesFromRive(riveViewModelInstance, viewModel);
    }

    public string DumpFileAsJson()
    {
        if (riveFile is null)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        riveFile.WriteRiveFileAsJson(sb);
        return sb.ToString();
    }

    protected override unsafe void DrawCore(RenderDrawContext context)
    {
        if (riveRenderContext is null || riveScene is null)
            return;

        var renderTarget = context.CommandList.RenderTarget;
        if (!IsSupportedByRive(renderTarget.Format))
        {
            logger.LogError($"The render target format '{renderTarget.Format}' is not supported by Rive.");
            return;
        }

        // Subscribe to input events - in case we have many sinks we assume that there's only one input source active
        var inputSource = context.RenderContext.Tags.Get(InputExtensions.WindowInputSource);
        if (inputSource != lastInputSource)
        {
            lastInputSource = inputSource;
            inputSubscription.Disposable = SubscribeToInputSource(inputSource, context);
        }

        var nativeRenderTarget = SharpDXInterop.GetNativeResource(renderTarget) as Texture2D;
        if (nativeRenderTarget is null)
            return;

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

    private static void ReadValuesFromRive(RiveViewModelInstance riveViewModelInstance, object? viewModel)
    {
        if (viewModel is IChannel channel)
        {
            if (channel.Object is object o)
            {
                if (ReadIntoObject(riveViewModelInstance, ref o))
                {
                    channel.Object = o;
                }
            }
        }
        else if (viewModel is not null && viewModel.GetType() != typeof(object))
        {
            ReadIntoObject(riveViewModelInstance, ref viewModel);
        }

        static bool ReadIntoObject(RiveViewModelInstance vm, ref object o)
        {
            var type = o.GetVLTypeInfo();

            var changed = false;
            foreach (var riveProp in vm.Properties)
            {
                var prop = type.GetProperty(riveProp.Name);
                if (prop is null)
                    continue;

                if (riveProp is RiveViewModelInstanceValue v)
                {
                    if (v.HasChanged && TryConvert(v.Value, prop.Type.ClrType, out var vlValue))
                    {
                        changed = true;
                        // Acknowledge the change
                        v.ClearChanges();
                        // Set the value on the object
                        o = prop.WithValue(o, vlValue);
                    }
                }
                else if (riveProp is RiveViewModelInstance vmi && prop.GetValue(o) is object sub)
                {
                    if (ReadIntoObject(vmi, ref sub))
                    {
                        changed = true;
                        // Set the value on the object
                        o = prop.WithValue(o, sub);
                    }
                }
            }
            return changed;
        }
    }

    private static void WriteValuesToRive(RiveViewModelInstance riveViewModel, object? viewModel)
    {
        if (viewModel is IChannel channel)
        {
            if (channel.Object is object o)
            {
                WriteFromObject(riveViewModel, o);
            }
        }
        else if (viewModel is not null && viewModel.GetType() != typeof(object))
        {
            WriteFromObject(riveViewModel, viewModel);
        }

        static void WriteFromObject(RiveViewModelInstance vm, object o)
        {
            var type = o.GetVLTypeInfo();
            foreach (var riveProp in vm.Properties)
            {
                var prop = type.GetProperty(riveProp.Name);
                if (prop is null)
                    continue;

                if (riveProp is RiveViewModelInstanceValue v)
                {
                    if (TryConvert(prop.GetValue(o), riveProp.Type, out var vlValue))
                    {
                        v.Value = vlValue;
                    }
                }
                else if (riveProp is RiveViewModelInstance vmi && prop.GetValue(o) is IVLObject sub)
                {
                    WriteFromObject(vmi, sub);
                }
            }
        }
    }

    private static bool TryConvert(object? v, Type type, [NotNullWhen(true)] out object? result)
    {
        try
        {
            result = Convert.ChangeType(v, type);
            return result is not null;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    protected override void Destroy()
    {
        viewModelSubscription.Dispose();
        inputSubscription.Dispose();

        DisposeRiveResources();

        base.Destroy();
    }

    private void DisposeRiveResources()
    {
        DisposeRiveFileResources();
        DisposeRiveRenderResources();
    }

    private void DisposeRiveFileResources()
    {
        DisposeAndSetNull(ref riveScene);
        DisposeAndSetNull(ref riveArtboard);
        DisposeAndSetNull(ref riveFile);
    }

    private void DisposeRiveRenderResources()
    {
        DisposeAndSetNull(ref riveRenderer);
        DisposeAndSetNull(ref riveRenderTarget);
        DisposeAndSetNull(ref riveRenderContext);
    }

    static void DisposeAndSetNull<T>(ref T? resource) where T : class, IDisposable => Interlocked.Exchange(ref resource, null)?.Dispose();
}
