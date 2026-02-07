using System.Collections.Immutable;
using System.Runtime.InteropServices;
using VL.Core;
using VL.Rive.Interop;

namespace VL.Rive.Internal;

internal sealed class RiveContext : IDisposable
{
    public RiveContext(NodeContext nodeContext, RiveFile riveFile)
    {
        NodeContext = nodeContext;
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

    public NodeContext NodeContext { get; }
    public RiveFile RiveFile { get; }
    public nint FactoryHandle => RiveFile.FactoryHandle;
    public TypeRegistry TypeRegistry => NodeContext.AppHost.TypeRegistry;
    public ImmutableArray<RiveViewModelType> ViewModels { get; }

    public void Dispose()
    {
        RiveFile.Dispose();
    }
}
