using Microsoft.Extensions.Logging;
using Stride.Engine;
using Stride.Graphics;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Basics.Video;
using VL.Stride.Graphics;
using Path = VL.Lib.IO.Path;
using Microsoft.Extensions.DependencyInjection;

namespace VL.Rive;

/// <summary>
/// Loads a Rive file. The file lives on the GPU device of the app, so it is loaded again when the device changes.
/// Everything made from the file (artboards, scenes, view model instances) goes away with it.
/// </summary>
[ProcessNode(Name = "RiveFile", Summary = "Loads a Rive file", Tags = "rive,riv,load")]
[Smell(SymbolSmell.Advanced)]
public sealed class RiveFileNode : IDisposable
{
    private readonly NodeContext nodeContext;
    private readonly ILogger logger;
    private readonly IGraphicsDeviceService graphicsDeviceService;
    private RiveContext? riveContext;
    private Path? path;

    public RiveFileNode(NodeContext nodeContext)
    {
        this.nodeContext = nodeContext;
        logger = nodeContext.GetLogger();
        graphicsDeviceService = nodeContext.AppHost.Services.GetRequiredService<Game>().Services.GetService<IGraphicsDeviceService>()
            ?? throw new InvalidOperationException("No IGraphicsDeviceService available.");
    }

    /// <param name="file">The .riv file</param>
    /// <param name="reload">Loads the file again</param>
    /// <param name="output">The loaded file, null while there is none. It is a new one every time the file is loaded again.</param>
    public void Update(Path? file, bool reload, out RiveContext? output)
    {
        // Native device can change - check on each update
        var graphicsDevice = graphicsDeviceService.GraphicsDevice;
        graphicsDevice.TryGetNativeDevicePointer(GraphicsDeviceType.Direct3D11, out var devicePointer);

        if (reload || file != path || (riveContext != null && riveContext.DevicePointer != devicePointer))
        {
            // Remember first: a file that does not load is reported once, not every frame
            path = file;
            Release();

            var filePath = file?.ToString();
            if (devicePointer != default && graphicsDevice.TryGetNativeDeviceContextPointer(GraphicsDeviceType.Direct3D11, out var deviceContextPointer) && !string.IsNullOrEmpty(filePath))
                riveContext = RiveContext.Create(nodeContext, logger, devicePointer, deviceContextPointer, filePath);
        }

        output = riveContext;
    }

    private void Release() => Interlocked.Exchange(ref riveContext, null)?.Dispose();

    public void Dispose() => Release();
}