using static RiveSharpInterop.Methods;

namespace VL.Rive;

internal class RiveRenderer : RiveObject
{
    public RiveRenderer(nint handle) : base(handle)
    {
    }
    protected override bool ReleaseHandle()
    {
        rive_Renderer_Destroy(handle);
        return true;
    }
}