using VL.Rive.Internal;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal class RiveFactory : RiveObject
{
    // The handle must exist before the base constructor runs: it rejects a null handle
    public RiveFactory() : base(Create())
    {
    }

    private static nint Create()
    {
        var handle = rive_Factory_Create();
        if (handle == nint.Zero)
            throw new InvalidOperationException("Failed to create Rive factory.");
        return handle;
    }

    public unsafe RiveFile LoadFile(string path)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        return Utils.LoadFile(path, handle);
    }

    protected override bool ReleaseHandle()
    {
        rive_Factory_Destroy(handle);
        return true;
    }
}
