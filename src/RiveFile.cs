using Microsoft.Extensions.Logging;
using VL.Core;
using VL.Rive.Internal;
using VL.Rive.Interop;

namespace VL.Rive;

/// <summary>
/// A loaded Rive file together with the render context and the recording host it was imported through. Images, fonts
/// and scripts of a file are created by the GPU device, which is why a file has a context of its own: it goes away,
/// with everything made from it (artboards, scenes, view model instances, render targets), when the file is reloaded
/// or the device changes. Renderers that show the same file take turns drawing with that one context.
/// </summary>
public sealed class RiveFile : IDisposable
{
    private readonly List<IDisposable> children = new();
    private readonly RiveRenderContextD3D11 renderContext;
    private RiveDeferredHost? deferredHost;
    private Interop.RiveRenderer? immediateRenderer;
    private RiveContext? context;

    private RiveFile(RiveRenderContextD3D11 renderContext, string filePath)
    {
        this.renderContext = renderContext;
        FilePath = filePath;
    }

    /// <summary>The path the file was loaded from.</summary>
    internal string FilePath { get; }

    internal bool IsDisposed { get; private set; }

    internal RiveContext Context => context ?? throw new ObjectDisposedException(nameof(RiveFile));
    internal RiveContext? GetContextOrNull() => IsDisposed ? null : context;
    internal RiveRenderContextD3D11 RenderContext => renderContext;
    internal nint DevicePointer => renderContext.DevicePointer;

    /// <summary>Records and replays the drawing of the file, null for a runtime without GPU scripting. Then draw through <see cref="ImmediateRenderer"/>.</summary>
    internal RiveDeferredHost? DeferredHost => deferredHost;
    internal Interop.RiveRenderer ImmediateRenderer => immediateRenderer ??= renderContext.CreateRenderer();

    internal static RiveFile Create(NodeContext? nodeContext, ILogger logger, nint device, nint deviceContext, string filePath)
    {
        var renderContext = RiveRenderContextD3D11.Create(device, deviceContext);
        var handle = new RiveFile(renderContext, filePath);
        try
        {
            // Files are imported through, and drawn by, a deferred recording host: only that can run the GPU
            // scripts of a file. Without it (a runtime built without the GPU layer) we draw immediately.
            handle.deferredHost = RiveDeferredHost.TryCreate(renderContext);
            if (handle.deferredHost is null)
                logger.LogWarning("GPU scripting is not available in this build of the Rive runtime.");

            ScriptLog.Attach(logger);
            var file = handle.deferredHost?.LoadFile(filePath) ?? renderContext.LoadFile(filePath);
            handle.context = new RiveContext(nodeContext, file);
            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    /// <summary>The artboard with the given name, the default one for no name. Null if there is none.</summary>
    internal RiveArtboard? GetArtboard(string? name)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        // What the file says about it, read off a short-lived instance
        var file = Context.RiveFile;
        using var instance = string.IsNullOrEmpty(name) ? file.GetArtboardDefault() : file.GetArtboard(name);
        if (instance is null)
            return null;
        return new RiveArtboard(this, string.IsNullOrEmpty(name) ? null : name, instance.Name, instance.ViewModelId);
    }

    /// <summary>A new instance of the artboard, of its own: it goes away with the file or when it is disposed.</summary>
    internal RiveArtboardInstance CreateArtboardInstance(RiveArtboard artboard)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var file = Context.RiveFile;
        var instance = (artboard.RequestedName is null ? file.GetArtboardDefault() : file.GetArtboard(artboard.RequestedName))
            ?? throw new InvalidOperationException($"Rive artboard \"{artboard.Name}\" not found in file \"{FilePath}\".");
        var artboardInstance = new RiveArtboardInstance(artboard, instance);
        Track(artboardInstance);
        return artboardInstance;
    }

    internal RiveRenderTargetD3D11 MakeRenderTarget(int width, int height)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var target = renderContext.MakeRenderTarget(width, height);
        Track(target);
        return target;
    }

    /// <summary>Disposes the resource when the file goes away, before the file does.</summary>
    internal void Track(IDisposable child)
    {
        lock (children)
            children.Add(child);
    }

    /// <summary>Called by a resource that has been disposed on its own.</summary>
    internal void Untrack(IDisposable child)
    {
        lock (children)
            children.Remove(child);
    }

    public override string ToString() => System.IO.Path.GetFileName(FilePath);

    void IDisposable.Dispose() => Dispose();

    internal void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;

        // Newest first: what was made from something goes before it
        List<IDisposable> toDispose;
        lock (children)
        {
            toDispose = new List<IDisposable>(children);
            children.Clear();
        }
        for (var i = toDispose.Count - 1; i >= 0; i--)
            toDispose[i].Dispose();

        Interlocked.Exchange(ref context, null)?.Dispose();
        Interlocked.Exchange(ref deferredHost, null)?.Dispose();
        Interlocked.Exchange(ref immediateRenderer, null)?.Dispose();
        renderContext.Dispose();
    }
}
