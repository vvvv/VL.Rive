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

    public unsafe string Name
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsClosed, this);
            return SpanExtensions.AsString(rive_Artboard_Name(handle));
        }
    }

    public int ViewModelId => (int)rive_Artboard_ViewModelId(handle);

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

    /// <summary>Resizes the artboard so its layout fills the given size. Needed before advancing for Fit.Layout.</summary>
    public void SetSize(float width, float height)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        rive_Artboard_SetSize(handle, width, height);
    }

    /// <summary>Back to the size the artboard has in the file.</summary>
    public void ResetSize()
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        rive_Artboard_ResetSize(handle);
    }

    protected override bool ReleaseHandle()
    {
        rive_ArtboardInstance_Destroy(handle);
        return true;
    }

        /// <summary>Binds the instance to the artboard, replacing the one it has. Null unbinds.</summary>
    public void BindViewModelInstance(RiveViewModel? riveViewModelInstance)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        rive_Artboard_BindViewModelInstance(handle, riveViewModelInstance?.InstanceHandle ?? nint.Zero);
    }
}