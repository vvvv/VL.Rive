using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal class RiveFile : RiveObject
{
    public RiveFile(nint handle) : base(handle) 
    {
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