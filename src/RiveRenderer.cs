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
using VL.Lib.Collections;
using VL.Lib.Reactive;
using VL.Rive;
using VL.Rive.Interop;
using VL.Stride.Input;
using Path = VL.Lib.IO.Path;
using PixelFormat = SharpDX.DXGI.Format;

namespace VL.Rive;

[ProcessNode(HasStateOutput = true, FragmentSelection = FragmentSelection.Explicit)]
public sealed partial class RiveRenderer : RendererBase
{
    readonly AppHost appHost;
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
        appHost = nodeContext.AppHost;
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
            lastViewModel = null;

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

    private void ReadValuesFromRive(RiveViewModelInstance riveViewModelInstance, object? viewModel)
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

        bool ReadIntoObject(RiveViewModelInstance vm, ref object o)
        {
            var type = o.GetVLTypeInfo();

            var changed = false;
            foreach (var riveProp in vm.Properties)
            {
                var riveValue = riveProp.Value;
                if (!IsSupportedRiveType(riveValue.RiveType))
                    continue;

                // ViewModelRuntimeInstance has no changed detection / not modeled as RuntimeValue internally
                var hasChanged = riveValue.HasChanged;
                if (hasChanged.HasValue && !hasChanged.Value)
                    continue;

                // Acknowledge the change
                riveValue.ClearChanges();

                var prop = type.GetProperty(riveProp.Name);
                if (prop is null)
                    continue;

                var value = riveValue.Value;
                if (value is RiveViewModelInstance vmi)
                {
                    if (prop.GetValue(o) is object sub)
                    {
                        if (ReadIntoObject(vmi, ref sub))
                        {
                            changed = true;
                            // Set the value on the object
                            o = prop.WithValue(o, sub);
                        }
                    }
                }
                else if (value is RiveViewModelList riveList)
                {
                    if (prop.GetValue(o) is ISpread spread)
                    {
                        var newSpread = spread.ToBuilder();
                        newSpread.Clear();
                        var i = 0;
                        foreach (var item in riveList)
                        {
                            if (i < spread.Count && spread.GetItem(i) is object sub)
                            {
                                if (ReadIntoObject(item, ref sub))
                                {
                                    newSpread.Add(sub);
                                    changed = true;
                                }
                                else
                                    newSpread.Add(sub); // No change
                            }
                            else
                            {
                                var typeInfo = appHost.TypeRegistry.GetTypeInfo(spread.ElementType);
                                var instance = appHost.CreateInstance(typeInfo);
                                if (instance != null)
                                {
                                    ReadIntoObject(item, ref instance);
                                    newSpread.Add(instance);
                                    changed = true;
                                }
                            }
                        }
                        o = prop.WithValue(o, riveList.ToSpread());
                    }
                }
                else if (TryConvert(value, prop.Type.ClrType, out var vlValue))
                {
                    changed = true;
                    // Set the value on the object
                    o = prop.WithValue(o, vlValue);
                }
            }
            return changed;
        }
    }

    private void WriteValuesToRive(RiveViewModelInstance riveViewModel, object? viewModel)
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

        void WriteFromObject(RiveViewModelInstance vm, object o)
        {
            var type = o.GetVLTypeInfo();
            foreach (var riveProp in vm.Properties)
            {
                var riveValue = riveProp.Value;
                if (!IsSupportedRiveType(riveValue.RiveType))
                    continue;

                var prop = type.GetProperty(riveProp.Name);
                if (prop is null)
                    continue;

                if (riveValue.Value is RiveViewModelInstance vmi)
                {
                    if (prop.GetValue(o) is object sub)
                    {
                        WriteFromObject(vmi, sub);
                    }
                }
                else if (riveValue.Value is RiveViewModelList riveList)
                {
                    if (prop.GetValue(o) is ISpread spread)
                    {
                        // Clear the existing list and populate with spread items
                        var riveCount = riveList.Count;
                        for (int i = 0; i < spread.Count; i++)
                        {
                            var item = spread.GetItem(i);
                            if (item is null)
                                continue;

                            if (i < riveCount)
                                WriteFromObject(riveList[i], item);
                            else
                            {
                                // Find view model
                                foreach (var viewModel in riveFile!.ViewModels)
                                {
                                    if (viewModel.Name == item.GetVLTypeInfo().Name)
                                    {
                                        var instance = riveFile.CreateViewModelInstance(viewModel.Name);
                                        riveList.Add(instance);
                                        WriteFromObject(instance, item);
                                        break;
                                    }
                                }
                            }
                        }
                        for (int i = riveCount - 1; i >= spread.Count; i--)
                            riveList.RemoveAt(i);
                    }
                }
                else if (TryConvert(prop.GetValue(o), riveValue.Type, out var vlValue))
                {
                    riveValue.Value = vlValue;
                }
            }
        }
    }

    private static bool IsSupportedRiveType(RiveDataType type) => type switch
    {
        RiveDataType.String => true,
        RiveDataType.Number => true,
        RiveDataType.Boolean => true,
        RiveDataType.Color => true,
        RiveDataType.Integer => true,
        RiveDataType.ViewModel => true,
        RiveDataType.List => true,
        _ => false,
    };

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
