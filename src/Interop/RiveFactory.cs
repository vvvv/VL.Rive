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

        var bytes = File.ReadAllBytes(path);
        fixed (byte* p = bytes)
        {
            // TODO: Check how it reacts if load fails
            var riveFileHandle = rive_File_Import(p, bytes.Length, handle);
            return new RiveFile(riveFileHandle);
        }
    }

    protected override bool ReleaseHandle()
    {
        rive_Factory_Destroy(handle);
        return true;
    }
}
