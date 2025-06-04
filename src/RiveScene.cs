using System.Runtime.InteropServices;
using VL.Rive.Interop;
using static VL.Rive.Interop.Methods;

namespace VL.Rive;

internal class RiveScene : RiveObject
{
    public unsafe RiveScene(nint handle) : base(handle) 
    {
        var namePtr = rive_Scene_Name(handle);
        Name = Marshal.PtrToStringAnsi((nint)namePtr) ?? string.Empty;
        NativeMemory.Free(namePtr);
    }

    public unsafe string Name { get; }

    public RiveAABB Bounds
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsClosed, this);
            return rive_Scene_Bounds(handle);
        }
    }

    public void AdvanceAndApply(float elapsedSeconds)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        rive_Scene_AdvanceAndApply(handle, elapsedSeconds);
    }

    public void Draw(RiveRenderer renderer)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        rive_Scene_Draw(handle, renderer.DangerousGetHandle());
    }

    public RiveHitResult PointerDown(float x, float y)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        return rive_Scene_PointerDown(handle, x, y);
    }

    public RiveHitResult PointerMove(float x, float y)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        return rive_Scene_PointerMove(handle, x, y);
    }

    public RiveHitResult PointerUp(float x, float y)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        return rive_Scene_PointerUp(handle, x, y);
    }

    public RiveHitResult PointerExit(float x, float y)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        return rive_Scene_PointerExit(handle, x, y);
    }

    protected override bool ReleaseHandle()
    {
        rive_Scene_Destroy(handle);
        return true;
    }

    internal void BindViewModelInstance(RiveViewModelInstance riveViewModelInstance)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        rive_Scene_BindViewModelInstance(handle, riveViewModelInstance.InstanceHandle);
    }
}