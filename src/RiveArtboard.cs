using static RiveSharpInterop.Methods;

namespace VL.Rive;

internal class RiveArtboard : RiveObject
{
    public RiveArtboard(nint handle) : base(handle) { }

    public RiveScene? DefaultScene()
    {
        var sceneHandle = rive_ArtboardInstance_DefaultScene(handle);
        if (sceneHandle == nint.Zero)
            return null;
        return new RiveScene(sceneHandle);
    }

    protected override bool ReleaseHandle()
    {
        rive_ArtboardInstance_Destroy(handle);
        return true;
    }
}