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
    public RiveContext(NodeContext? nodeContext, RiveFile riveFile)
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

    public NodeContext NodeContext => nodeContext ?? throw new InvalidOperationException("This Rive context was created without a VL node context.");
    public RiveFile RiveFile { get; }
    public nint FactoryHandle => RiveFile.FactoryHandle;
    public TypeRegistry TypeRegistry => NodeContext.AppHost.TypeRegistry;
    public ImmutableArray<RiveViewModelType> ViewModels { get; }

    public void Dispose()
    {
        RiveFile.Dispose();
    }
}
