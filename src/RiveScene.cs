using static RiveSharpInterop.Methods;

namespace VL.Rive;

internal class RiveScene : RiveObject
{
    public RiveScene(nint handle) : base(handle) { }

    public void AdvanceAndApply(float elapsedSeconds)
    {
        rive_Scene_AdvanceAndApply(handle, elapsedSeconds);
    }

    public void Draw(RiveRenderer renderer, int width, int height)
    {
        rive_Scene_Draw(handle, renderer.DangerousGetHandle(), width, height);
    }

    protected override bool ReleaseHandle()
    {
        rive_Scene_Destroy(handle);
        return true;
    }
}