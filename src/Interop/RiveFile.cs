using System.Collections.Immutable;
using System.Runtime.InteropServices;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal class RiveFile : RiveObject
{
    private ImmutableArray<RiveViewModel> viewModels;

    public RiveFile(nint handle) : base(handle) 
    {
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

    public RiveArtboard GetArtboardDefault()
    {
        var artboardHandle = rive_File_GetArtboardDefault(handle);
        if (artboardHandle == nint.Zero)
            throw new InvalidOperationException("Failed to get default artboard from Rive file.");
        return new RiveArtboard(artboardHandle);
    }

    public RiveViewModelInstance? DefaultArtboardViewModel(RiveArtboard artboard)
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