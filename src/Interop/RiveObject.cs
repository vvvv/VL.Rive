using System.Runtime.InteropServices;

namespace VL.Rive.Interop;

internal abstract class RiveObject : SafeHandle
{
    protected RiveObject(nint handle) : base(nint.Zero, true) 
    {
        SetHandle(handle);
    }

    public override bool IsInvalid => handle == nint.Zero || IsClosed;
}
