using System.Runtime.InteropServices;

namespace VL.Rive.Interop;

abstract class RiveObject : SafeHandle
{
    protected RiveObject(nint handle, bool ownsHandle = true) : base(nint.Zero, ownsHandle) 
    {
        if (handle == default)
            throw new ArgumentNullException(nameof(handle));

        SetHandle(handle);
    }

    public override bool IsInvalid => handle == nint.Zero || IsClosed;
}
