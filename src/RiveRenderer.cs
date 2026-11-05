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
public sealed class RiveRenderer : RendererBase
{
    readonly NodeContext nodeContext;
    readonly ILogger logger;
    readonly RiveSceneDrawer drawer;
    readonly ArtboardSlot artboardSlot = new();
    readonly RiveSceneSlot slot;
    readonly IGraphicsDeviceService graphicsDeviceService;

    RiveFile? riveFile;
    Path? riveFilePath;

    [Fragment]
    [Smell(SymbolSmell.Internal)]
    public RiveRenderer([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    {
        this.nodeContext = nodeContext;
        logger = nodeContext.GetLogger();
        drawer = new RiveSceneDrawer(logger);
        slot = new RiveSceneSlot(nodeContext);
        graphicsDeviceService = nodeContext.AppHost.Services.GetRequiredService<Game>().Services.GetService<IGraphicsDeviceService>()
            ?? throw new InvalidOperationException("No IGraphicsDeviceService available.");
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
        drawer.Fit = fit;
        drawer.Alignment = alignment;
        drawer.Frame = frame;
        drawer.Content = content;
        drawer.ScaleFactor = scaleFactor;

        // Native device can change - check on each update
        var nativeDevice = SharpDXInterop.GetNativeDevice(graphicsDeviceService.GraphicsDevice) as Device;
        var filePath = file?.ToString();
        if (reload || file != riveFilePath || (riveFile != null && riveFile.DevicePointer != nativeDevice?.NativePointer))
        {
            riveFilePath = file;
            DisposeFile();

            if (nativeDevice != null && !string.IsNullOrEmpty(filePath))
                riveFile = RiveFile.Create(nodeContext, logger, nativeDevice.NativePointer, nativeDevice.ImmediateContext.NativePointer, filePath);
        }

        drawer.Scene = slot.Update(artboardSlot.Update(riveFile, artboardName), sceneName, null);
    }

    [Fragment(IsDefault = true)]
    [Smell(SymbolSmell.Internal)]
    public RiveViewModel? ViewModel => slot.BoundViewModel;

    public string DumpFileAsJson()
    {
        if (riveFile is null || riveFile.IsDisposed)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        riveFile.Context.RiveFile.WriteRiveFileAsJson(sb);
        return sb.ToString();
    }

    public RectangleF GetArtboardBounds()
    {
        var artboard = slot.Instance;
        if (artboard is null)
            return RectangleF.Empty;
        var bounds = artboard.Bounds;
        return new RectangleF(bounds.minX, bounds.minY, bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
    }

    protected override void DrawCore(RenderDrawContext context) => drawer.Draw(context);

    protected override void Destroy()
    {
        drawer.Dispose();
        slot.Dispose();
        DisposeFile();

        base.Destroy();
    }

    private void DisposeFile()
    {
        // The slot's artboard goes away with the file, forget it before
        slot.Update(null, null, null);
        artboardSlot.Update(null, null);
        drawer.Scene = null;
        Interlocked.Exchange(ref riveFile, null)?.Dispose();
    }
}