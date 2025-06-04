using static VL.Rive.Interop.Methods;

namespace VL.Rive;

internal class RiveFile : RiveObject
{
    public static RiveFile FromHandle(nint handle)
    {
        if (handle == nint.Zero)
            throw new ArgumentNullException(nameof(handle), "Rive file handle cannot be null.");
        return new RiveFile(handle);
    }

    public RiveFile(nint handle) : base(handle) { }

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

        return new RiveViewModelInstance(rive_ViewModelRuntime_CreateInstance(viewModelRuntime));
    }

    protected override bool ReleaseHandle()
    {
        rive_File_Destroy(handle);
        return true;
    }
}