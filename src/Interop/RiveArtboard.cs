using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

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

    public void BindViewModelInstance(RiveViewModelInstance riveViewModelInstance)
    {
        rive_Artboard_BindViewModelInstance(handle, riveViewModelInstance.InstanceHandle);
    }
}