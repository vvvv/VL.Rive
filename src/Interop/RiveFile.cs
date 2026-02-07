using System.Collections.Immutable;
using System.Runtime.InteropServices;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal unsafe class RiveFile : RiveObject
{
    private ImmutableArray<RiveArtboardInfo> artboards;
    private ImmutableArray<RiveViewModelInfo> viewModels;
    private readonly nint factoryHandle;

    public RiveFile(nint handle, nint factoryHandle) : base(handle) 
    {
        this.factoryHandle = factoryHandle;
    }

    public nint FactoryHandle => factoryHandle;

    public ImmutableArray<RiveArtboardInfo> Artboards
    {
        get
        {
            if (artboards.IsDefault)
                artboards = GetArtboards();
            return artboards;

            unsafe ImmutableArray<RiveArtboardInfo> GetArtboards()
            {
                var count = rive_File_ArtboardCount(handle);
                var nativeArtboards = stackalloc nint[count];
                rive_File_Artboards(handle, nativeArtboards);
                var artboards = ImmutableArray.CreateBuilder<RiveArtboardInfo>(count);
                for (var i = 0; i < count; i++)
                {
                    var artboard = GetArtboard(nativeArtboards[i]);
                    artboards.Add(artboard);
                }
                return artboards.ToImmutable();
            }

            unsafe RiveArtboardInfo GetArtboard(nint nativeArtboard)
            {
                // State Machines
                var stateMachineCount = rive_Artboard_StateMachineCount(nativeArtboard);
                var nativeStateMachines = stackalloc nint[stateMachineCount];
                rive_Artboard_StateMachines(nativeArtboard, nativeStateMachines);
                var stateMachines = ImmutableArray.CreateBuilder<RiveStateMachineInfo>(stateMachineCount);
                for (var j = 0; j < stateMachineCount; j++)
                {
                    var nativeStateMachine = nativeStateMachines[j];
                    stateMachines.Add(GetStateMachine(nativeStateMachine));
                }

                // Animations
                var animations = ImmutableArray.CreateBuilder<RiveAnimationInfo>(rive_Artboard_AnimationCount(nativeArtboard));
                var nativeAnimations = stackalloc nint[rive_Artboard_AnimationCount(nativeArtboard)];
                rive_Artboard_Animations(nativeArtboard, nativeAnimations);
                for (var j = 0; j < rive_Artboard_AnimationCount(nativeArtboard); j++)
                {
                    var nativeAnimation = nativeAnimations[j];
                    animations.Add(GetAnimation(nativeAnimation));
                }

                var defaultViewModelId = (int)rive_Artboard_ViewModelId(nativeArtboard);
                var defaultViewModelName = ViewModels.ElementAtOrDefault(defaultViewModelId).Name;

                var name = SpanExtensions.AsString(rive_Artboard_Name(nativeArtboard));
                return new RiveArtboardInfo(name, stateMachines.ToImmutable(), animations.ToImmutable(), defaultViewModelName);
            }

            unsafe RiveStateMachineInfo GetStateMachine(nint nativeStateMachine)
            {
                var name = SpanExtensions.AsString(rive_StateMachine_Name(nativeStateMachine));
                return new RiveStateMachineInfo(name);
            }

            unsafe RiveAnimationInfo GetAnimation(nint nativeAnimation)
            {
                var name = SpanExtensions.AsString(rive_Animation_Name(nativeAnimation));
                return new RiveAnimationInfo(name);
            }
        }
    }

    public ImmutableArray<RiveViewModelInfo> ViewModels
    {
        get
        {
            if (viewModels.IsDefault)
            {
                var count = rive_File_ViewModelCount(handle);
                var models = ImmutableArray.CreateBuilder<RiveViewModelInfo>(count);
                for (var i = 0; i < count; i++)
                    models.Add(GetViewModel(i));
                viewModels = models.ToImmutableArray();
            }
            return viewModels;

            unsafe RiveViewModelInfo GetViewModel(int i)
            {
                sbyte* namePtr;
                int propertyCount;
                rive_File_GetViewModel(handle, i, &namePtr, &propertyCount);

                try
                {
                    var props = stackalloc RivePropertyData[propertyCount];
                    rive_File_GetViewModelProperties(handle, i, props);

                    var builder = ImmutableArray.CreateBuilder<RivePropertyInfo>(propertyCount);
                    for (int j = 0; j < propertyCount; j++)
                    {
                        var nativeProperty = props[j];
                        try
                        {
                            var property = new RivePropertyInfo(SpanExtensions.AsString(nativeProperty.name), (RiveDataType)nativeProperty.type, nativeProperty.viewModelReferenceId);
                            builder.Add(property);
                        }
                        finally
                        {
                            if (nativeProperty.name != null)
                                NativeMemory.Free(nativeProperty.name);
                        }
                    }

                    return new RiveViewModelInfo(SpanExtensions.AsString(namePtr), builder.ToImmutable());
                }
                finally
                {
                    if (namePtr != null)
                        NativeMemory.Free(namePtr);
                }
            }
        }
    }

    public RiveArtboardInstance GetArtboardDefault()
    {
        var artboardHandle = rive_File_GetArtboardDefault(handle);
        if (artboardHandle == nint.Zero)
            throw new InvalidOperationException("Failed to get default artboard from Rive file.");
        return new RiveArtboardInstance(artboardHandle);
    }

    public unsafe RiveArtboardInstance? GetArtboard(string name)
    {
        using var marshaledName = new MarshaledString(name);
        var artboardHandle = rive_File_ArtboardByName(handle, marshaledName.Value);
        if (artboardHandle == nint.Zero)
            return null;
        return new RiveArtboardInstance(artboardHandle);
    }

    public nint CreateViewModelRuntime(string name)
    {
        using var marshaledName = new MarshaledString(name);
        return rive_File_ViewModelByName(handle, marshaledName.Value);
    }

    public nint CreateViewModelRuntime(int index)
    {
        return rive_File_ViewModelByIndex(handle, index);
    }

    protected override bool ReleaseHandle()
    {
        rive_File_Destroy(handle);
        return true;
    }
}