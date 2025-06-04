using static VL.Rive.Interop.Methods;

namespace VL.Rive;

internal abstract class RiveRenderTarget : RiveObject
{
    public RiveRenderTarget(nint handle) : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        rive_RenderTarget_Destroy(handle);
        return true;
    }
}
