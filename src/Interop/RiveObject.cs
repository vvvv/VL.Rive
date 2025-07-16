using System.Runtime.InteropServices;

namespace VL.Rive.Interop;

internal abstract class RiveObject : SafeHandle
{
    protected RiveObject(nint handle, bool ownsHandle = true) : base(nint.Zero, ownsHandle) 
    {
        SetHandle(handle);
    }

    public override bool IsInvalid => handle == nint.Zero || IsClosed;
}
