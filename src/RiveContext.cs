using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using VL.Core;
using VL.Core.Import;
using VL.Rive.Internal;
using VL.Rive.Interop;

namespace VL.Rive;

/// <summary>
/// A loaded Rive file: what the file says (its view models, and the instances made from them) together with the
/// render context and the recording host it was imported through. Images, fonts and scripts of a file are created by
/// the GPU device, which is why a file has a render context of its own: it goes away, with everything made from it
/// (artboards, scenes, view model instances, render targets), when the file is reloaded or the device changes.
/// Renderers that show the same file take turns drawing with that one render context.
/// </summary>
public sealed class RiveContext : IDisposable
{
    private readonly NodeContext? nodeContext;
    private readonly List<IDisposable> children = new();
    private readonly RiveRenderContextD3D11? renderContext;
    private RiveDeferredHost? deferredHost;
    private Interop.RiveRenderer? immediateRenderer;

    /// <param name="nodeContext">
    /// The VL node context, needed only where VL asks for type information or the host (<see cref="NodeContext"/>).
    /// Null in tests, which exercise the Rive side without a VL app host.
    /// </param>
    internal RiveContext(NodeContext? nodeContext, RiveFile riveFile) : this(nodeContext, riveFile, null, null, null)
    {
    }

    private RiveContext(NodeContext? nodeContext, RiveFile riveFile, string? filePath, RiveRenderContextD3D11? renderContext, RiveDeferredHost? deferredHost)
    {
        this.nodeContext = nodeContext;
        RiveFile = riveFile;
        FilePath = filePath;
        this.renderContext = renderContext;
        this.deferredHost = deferredHost;
        ViewModels = CreateViewModels();

        ImmutableArray<RiveViewModelType> CreateViewModels()
        {
            var viewModelCount = riveFile.ViewModels.Length;
            var viewModels = new RiveViewModelType[viewModelCount];
            for (var i = 0; i < viewModelCount; i++)
            {
                var riveViewModel = riveFile.ViewModels[i];
                var handle = Methods.rive_File_ViewModelByIndex(riveFile.DangerousGetHandle(), i);
                viewModels[i] = new RiveViewModelType(this, handle, riveViewModel);
            }
            return ImmutableCollectionsMarshal.AsImmutableArray(viewModels);
        }
    }

    /// <summary>Loads a file on the given GPU device.</summary>
    internal static RiveContext Create(NodeContext? nodeContext, ILogger logger, nint device, nint deviceContext, string filePath)
    {
        var renderContext = RiveRenderContextD3D11.Create(device, deviceContext);
        RiveDeferredHost? deferredHost = null;
        RiveFile? file = null;
        try
        {
            // Files are imported through, and drawn by, a deferred recording host: only that can run the GPU
            // scripts of a file. Without it (a runtime built without the GPU layer) we draw immediately.
            deferredHost = RiveDeferredHost.TryCreate(renderContext);
            if (deferredHost is null)
                logger.LogWarning("GPU scripting is not available in this build of the Rive runtime.");

            ScriptLog.Attach(logger);
            file = deferredHost?.LoadFile(filePath) ?? renderContext.LoadFile(filePath);
            return new RiveContext(nodeContext, file, filePath, renderContext, deferredHost);
        }
        catch
        {
            // What was imported through the host and the render context goes before them
            file?.Dispose();
            deferredHost?.Dispose();
            renderContext.Dispose();
            throw;
        }
    }

    private readonly Dictionary<nint, WeakReference<RiveViewModel>> viewModels = new();

    /// <summary>
    /// Gives the one view model object for a native instance. Takes over the native reference the caller holds:
    /// it goes to a new object, or is released when the instance already has one.
    /// </summary>
    internal RiveViewModel WrapViewModel(nint ownedHandle)
    {
        var identity = Methods.rive_ViewModelInstanceRuntime_Instance(ownedHandle);
        lock (viewModels)
        {
            if (viewModels.TryGetValue(identity, out var weak) && weak.TryGetTarget(out var existing) && !existing.IsDisposed)
            {
                Methods.rive_ViewModelInstanceRuntime_Release(ownedHandle);
                return existing;
            }

            var viewModel = new RiveViewModel(this, ownedHandle);
            viewModels[identity] = new WeakReference<RiveViewModel>(viewModel);
            return viewModel;
        }
    }

    internal void ForgetViewModel(nint identity, RiveViewModel viewModel)
    {
        lock (viewModels)
        {
            // Not when the instance has a newer object by now
            if (viewModels.TryGetValue(identity, out var weak) && (!weak.TryGetTarget(out var current) || ReferenceEquals(current, viewModel)))
                viewModels.Remove(identity);
        }
    }

    /// <summary>
    /// Tells the listeners of every view model object of the file what has changed since the last time. Reaches what
    /// a bound instance only holds, such as the items of its lists: an item has an object once somebody asked for it,
    /// and only then somebody can listen to it.
    /// </summary>
    internal void AcknowledgeChanges()
    {
        // Listeners may ask for more objects while they are told
        List<RiveViewModel> alive;
        lock (viewModels)
            alive = viewModels.Values.Select(weak => weak.TryGetTarget(out var viewModel) ? viewModel : null).OfType<RiveViewModel>().ToList();
        foreach (var viewModel in alive)
            viewModel.AcknowledgeChanges();
    }

    internal NodeContext NodeContext => nodeContext ?? throw new InvalidOperationException("This Rive context was created without a VL node context.");
    internal RiveFile RiveFile { get; }
    internal nint FactoryHandle => RiveFile.FactoryHandle;
    internal TypeRegistry TypeRegistry => NodeContext.AppHost.TypeRegistry;
    internal ImmutableArray<RiveViewModelType> ViewModels { get; }

    internal RiveViewModelType? FindViewModel(string name) => ViewModels.FirstOrDefault(type => type.Name == name);

    /// <summary>The path the file was loaded from, null if it was not loaded from a path (in tests).</summary>
    internal string? FilePath { get; }

    internal bool IsDisposed { get; private set; }

    // Only a context that was loaded on a GPU device has these, not the ones of the tests
    internal RiveRenderContextD3D11 RenderContext => renderContext ?? throw new InvalidOperationException("This Rive context was created without a render context.");
    internal nint DevicePointer => RenderContext.DevicePointer;

    /// <summary>Records and replays the drawing of the file, null for a runtime without GPU scripting. Then draw through <see cref="ImmediateRenderer"/>.</summary>
    internal RiveDeferredHost? DeferredHost => deferredHost;
    internal Interop.RiveRenderer ImmediateRenderer => immediateRenderer ??= RenderContext.CreateRenderer();

    /// <summary>What the file says about the artboard with the given name, the default one for no name. Null if there is none.</summary>
    internal RiveArtboardInfo? GetArtboardInfo(string? name)
    {
        if (IsDisposed)
            return null;

        if (string.IsNullOrEmpty(name))
        {
            // The default one is told by the artboard itself
            using var defaultArtboard = RiveFile.GetArtboardDefault();
            name = defaultArtboard?.Name;
        }
        foreach (var info in RiveFile.Artboards)
            if (info.Name == name)
                return info;
        return null;
    }

    internal RiveRenderTargetD3D11 MakeRenderTarget(int width, int height)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var target = RenderContext.MakeRenderTarget(width, height);
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

    [Smell(SymbolSmell.Hidden)]
    public override string ToString() => FilePath is null ? "Rive file" : System.IO.Path.GetFileName(FilePath);

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

        // The instances belong to the file: dispose them while it is still there, so that using one later
        // throws instead of touching a file that is gone
        List<RiveViewModel> instances;
        lock (viewModels)
        {
            instances = viewModels.Values.Select(weak => weak.TryGetTarget(out var viewModel) ? viewModel : null).OfType<RiveViewModel>().ToList();
            viewModels.Clear();
        }
        foreach (var instance in instances)
            instance.Dispose();

        RiveFile.Dispose();
        Interlocked.Exchange(ref deferredHost, null)?.Dispose();
        Interlocked.Exchange(ref immediateRenderer, null)?.Dispose();
        renderContext?.Dispose();
    }
}