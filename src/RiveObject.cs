using System.Runtime.InteropServices;

namespace VL.Rive
{
    internal abstract class RiveObject : SafeHandle
    {
        protected RiveObject(nint handle) : base(IntPtr.Zero, true) 
        {
            SetHandle(handle);
        }

        public override bool IsInvalid => handle == IntPtr.Zero || IsClosed;
    }
}
