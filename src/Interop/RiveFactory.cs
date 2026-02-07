using VL.Rive.Internal;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal class RiveFactory : RiveObject
{
    public RiveFactory() : base(default)
    {
        var handle = rive_Factory_Create();
        if (handle == nint.Zero)
            throw new InvalidOperationException("Failed to create Rive factory.");

        SetHandle(handle);
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
