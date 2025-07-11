using CommunityToolkit.HighPerformance.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal abstract class RiveRenderContext : RiveObject
{
    public RiveRenderContext(nint handle) : base(handle)
    {
    }

    public unsafe RiveFile LoadFile(string path)
    {
        // TODO: Check how it reacts if load fails

        using Stream fileStream = File.OpenRead(path);
        if (fileStream.Length <= int.MaxValue)
        {
            using var bytes = MemoryOwner<byte>.Allocate((int)fileStream.Length);
            fileStream.Read(bytes.Span);
            fixed (byte* p = bytes.Span)
            {
                var riveFileHandle = rive_File_Import(p, bytes.Length, handle);
                return new RiveFile(riveFileHandle);
            }
        }
        else
        {
            var bytes = File.ReadAllBytes(path);
            fixed (byte* p = bytes)
            {
                var riveFileHandle = rive_File_Import(p, bytes.Length, handle);
                return new RiveFile(riveFileHandle);
            }
        }
    }

    public RiveRenderer CreateRenderer()
    {
        var renderer = rive_Renderer_Create(handle);
        if (renderer == nint.Zero)
        {
            throw new InvalidOperationException("Failed to create RiveRenderer.");
        }
        return new RiveRenderer(renderer);
    }

    public unsafe void BeginFrame(ref readonly FrameDescriptor frameDescriptor)
    {
        rive_RenderContext_BeginFrame(handle, (FrameDescriptor*)Unsafe.AsPointer(ref Unsafe.AsRef(in frameDescriptor)));
    }

    public void Flush(RiveRenderTarget renderTarget)
    {
        rive_RenderContext_Flush(handle, renderTarget.DangerousGetHandle());
    }

    protected override bool ReleaseHandle()
    {
        rive_RenderContext_Destroy(handle);
        return true;
    }
}