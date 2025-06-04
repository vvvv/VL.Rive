using VL.Rive.Interop;
using System.Runtime.CompilerServices;
using static VL.Rive.Interop.Methods;

namespace VL.Rive;

internal class RiveRenderer : RiveObject
{
    public RiveRenderer(nint handle) : base(handle)
    {
    }

    public void Save()
    {
        rive_Renderer_Save(handle);
    }

    public unsafe void Transform(ref readonly RiveMat2D mat)
    {
        rive_Renderer_Transform(handle, (RiveMat2D*)Unsafe.AsPointer(ref Unsafe.AsRef(in mat)));
    }

    public void Restore()
    {
        rive_Renderer_Restore(handle);
    }

    protected override bool ReleaseHandle()
    {
        rive_Renderer_Destroy(handle);
        return true;
    }
}