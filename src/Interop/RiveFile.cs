using System.Collections.Immutable;
using System.Runtime.InteropServices;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal class RiveFile : RiveObject
{
    private ImmutableArray<RiveArtboard> artboards;
    private ImmutableArray<RiveViewModel> viewModels;

    public RiveFile(nint handle) : base(handle) 
    {
    }

    public ImmutableArray<RiveArtboard> Artboards
    {
        get
        {
            if (artboards.IsDefault)
                artboards = GetArtboards();
            return artboards;

            unsafe ImmutableArray<RiveArtboard> GetArtboards()
            {
                var count = rive_File_ArtboardCount(handle);
                var nativeArtboards = stackalloc nint[count];
                rive_File_Artboards(handle, nativeArtboards);
                var artboards = ImmutableArray.CreateBuilder<RiveArtboard>(count);
                for (var i = 0; i < count; i++)
                {
                    var artboard = GetArtboard(nativeArtboards[i]);
                    artboards.Add(artboard);
                }
                return artboards.ToImmutable();
            }

            unsafe RiveArtboard GetArtboard(nint nativeArtboard)
            {
                // State Machines
                var stateMachineCount = rive_Artboard_StateMachineCount(nativeArtboard);
                var nativeStateMachines = stackalloc nint[stateMachineCount];
                rive_Artboard_StateMachines(nativeArtboard, nativeStateMachines);
                var stateMachines = ImmutableArray.CreateBuilder<RiveStateMachine>(stateMachineCount);
                for (var j = 0; j < stateMachineCount; j++)
                {
                    var nativeStateMachine = nativeStateMachines[j];
                    stateMachines.Add(GetStateMachine(nativeStateMachine));
                }

                // Animations
                var animations = ImmutableArray.CreateBuilder<RiveAnimation>(rive_Artboard_AnimationCount(nativeArtboard));
                var nativeAnimations = stackalloc nint[rive_Artboard_AnimationCount(nativeArtboard)];
                rive_Artboard_Animations(nativeArtboard, nativeAnimations);
                for (var j = 0; j < rive_Artboard_AnimationCount(nativeArtboard); j++)
                {
                    var nativeAnimation = nativeAnimations[j];
                    animations.Add(GetAnimation(nativeAnimation));
                }

                var name = new string(rive_Artboard_Name(nativeArtboard));
                return new RiveArtboard(name, stateMachines.ToImmutable(), animations.ToImmutable());
            }

            unsafe RiveStateMachine GetStateMachine(nint nativeStateMachine)
            {
                var name = new string(rive_StateMachine_Name(nativeStateMachine));
                return new RiveStateMachine(name);
            }

            unsafe RiveAnimation GetAnimation(nint nativeAnimation)
            {
                var name = new string(rive_Animation_Name(nativeAnimation));
                return new RiveAnimation(name);
            }
        }
    }

    public ImmutableArray<RiveViewModel> ViewModels
    {
        get
        {
            if (viewModels.IsDefault)
            {
                var count = rive_File_ViewModelCount(handle);
                var models = ImmutableArray.CreateBuilder<RiveViewModel>(count);
                for (var i = 0; i < count; i++)
                    models.Add(GetViewModel(i));
                viewModels = models.ToImmutableArray();
            }
            return viewModels;

            unsafe RiveViewModel GetViewModel(int i)
            {
                sbyte* namePtr;
                int propertyCount;
                rive_File_GetViewModel(handle, i, &namePtr, &propertyCount);

                try
                {
                    var props = stackalloc RivePropertyData[propertyCount];
                    rive_File_GetViewModelProperties(handle, i, props);

                    var builder = ImmutableArray.CreateBuilder<PropertyData>(propertyCount);
                    for (int j = 0; j < propertyCount; j++)
                    {
                        var nativeProperty = props[j];
                        try
                        {
                            var propertyData = new PropertyData(Marshal.PtrToStringAnsi((nint)nativeProperty.name) ?? string.Empty, (RiveDataType)nativeProperty.type, nativeProperty.viewModelReferenceId);
                            builder.Add(propertyData);
                        }
                        finally
                        {
                            if (nativeProperty.name != null)
                                NativeMemory.Free(nativeProperty.name);
                        }
                    }

                    return new RiveViewModel(Marshal.PtrToStringAnsi((nint)namePtr) ?? string.Empty, builder.ToImmutable());
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

    public RiveViewModelInstance? DefaultArtboardViewModel(RiveArtboardInstance artboard)
    {
        var viewModelRuntime = rive_File_DefaultArtboardViewModel(handle, artboard.DangerousGetHandle());
        if (viewModelRuntime == default)
            return null;

        return new RiveViewModelInstance(string.Empty, rive_ViewModelRuntime_CreateDefaultInstance(viewModelRuntime));
    }

    protected override bool ReleaseHandle()
    {
        rive_File_Destroy(handle);
        return true;
    }
}