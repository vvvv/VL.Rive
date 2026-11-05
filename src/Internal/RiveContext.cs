using System.Collections.Immutable;
using System.Runtime.InteropServices;
using VL.Core;
using VL.Rive.Interop;

namespace VL.Rive.Internal;

internal sealed class RiveContext : IDisposable
{
    private readonly NodeContext? nodeContext;

    /// <param name="nodeContext">
    /// The VL node context, needed only where VL asks for type information or the host (<see cref="NodeContext"/>).
    /// Null in tests, which exercise the Rive side without a VL app host.
    /// </param>
    public RiveContext(NodeContext? nodeContext, NativeRiveFile riveFile)
    {
        this.nodeContext = nodeContext;
        RiveFile = riveFile;
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

    private readonly Dictionary<nint, WeakReference<RiveViewModel>> viewModels = new();

    /// <summary>
    /// Gives the one view model object for a native instance. Takes over the native reference the caller holds:
    /// it goes to a new object, or is released when the instance already has one.
    /// </summary>
    public RiveViewModel WrapViewModel(nint ownedHandle)
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

    public void ForgetViewModel(nint identity, RiveViewModel viewModel)
    {
        lock (viewModels)
        {
            // Not when the instance has a newer object by now
            if (viewModels.TryGetValue(identity, out var weak) && (!weak.TryGetTarget(out var current) || ReferenceEquals(current, viewModel)))
                viewModels.Remove(identity);
        }
    }

    public NodeContext NodeContext => nodeContext ?? throw new InvalidOperationException("This Rive context was created without a VL node context.");
    public NativeRiveFile RiveFile { get; }
    public nint FactoryHandle => RiveFile.FactoryHandle;
    public TypeRegistry TypeRegistry => NodeContext.AppHost.TypeRegistry;
    public ImmutableArray<RiveViewModelType> ViewModels { get; }

    public RiveViewModelType? FindViewModel(string name) => ViewModels.FirstOrDefault(type => type.Name == name);

    public void Dispose()
    {
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
    }
}
