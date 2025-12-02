using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal class RiveArtboardInstance : RiveObject
{
    public RiveArtboardInstance(nint handle) : base(handle) { }

    public RiveAABB Bounds
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsClosed, this);
            return rive_Artboard_Bounds(handle);
        }
    }

    public RiveScene? GetDefaultScene()
    {
        var sceneHandle = rive_ArtboardInstance_DefaultScene(handle);
        if (sceneHandle == nint.Zero)
            return null;
        return new RiveScene(sceneHandle);
    }

    public unsafe RiveScene? GetScene(string name)
    {
        using var marshaledName = new MarshaledString(name);
        var sceneHandle = rive_ArtboardInstance_SceneByName(handle, marshaledName.Value);
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