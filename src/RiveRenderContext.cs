using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static RiveSharpInterop.Methods;

namespace VL.Rive;

internal abstract class RiveRenderContext : RiveObject
{
    public RiveRenderContext(nint handle) : base(handle)
    {
    }

    public unsafe RiveFile LoadFile(string file)
    {
        var bytes = File.ReadAllBytes(file);
        fixed (byte* p = bytes)
        {
            return RiveFile.FromHandle(rive_File_Import(p, bytes.Length, handle));
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

    public unsafe void BeginFrame(ref readonly RiveSharpInterop.FrameDescriptor frameDescriptor)
    {
        rive_RenderContext_BeginFrame(handle, (RiveSharpInterop.FrameDescriptor*)Unsafe.AsPointer(ref Unsafe.AsRef(in frameDescriptor)));
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