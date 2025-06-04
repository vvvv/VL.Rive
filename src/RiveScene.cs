using VL.Rive.Interop;
using static VL.Rive.Interop.Methods;

namespace VL.Rive;

internal class RiveScene : RiveObject
{
    public RiveScene(nint handle) : base(handle) { }

    public RiveAABB Bounds => rive_Scene_Bounds(handle);

    public void AdvanceAndApply(float elapsedSeconds)
    {
        rive_Scene_AdvanceAndApply(handle, elapsedSeconds);
    }

    public void Draw(RiveRenderer renderer)
    {
        rive_Scene_Draw(handle, renderer.DangerousGetHandle());
    }

    public RiveHitResult PointerDown(float x, float y) => rive_Scene_PointerDown(handle, x, y);
    public RiveHitResult PointerMove(float x, float y) => rive_Scene_PointerMove(handle, x, y);
    public RiveHitResult PointerUp(float x, float y) => rive_Scene_PointerUp(handle, x, y);
    public RiveHitResult PointerExit(float x, float y) => rive_Scene_PointerExit(handle, x, y);

    protected override bool ReleaseHandle()
    {
        rive_Scene_Destroy(handle);
        return true;
    }
}