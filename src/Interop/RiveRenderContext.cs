using System.Runtime.CompilerServices;
using VL.Rive.Internal;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal abstract class RiveRenderContext : RiveObject
{
    public RiveRenderContext(nint handle) : base(handle)
    {
    }

    public unsafe RiveFile LoadFile(string path) => Utils.LoadFile(path, handle);

    public RiveRenderer CreateRenderer()
    {
        var renderer = rive_Renderer_Create(handle);
        if (renderer == nint.Zero)
        {
            throw new InvalidOperationException("Failed to create RiveRenderer.");
        }
        return new RiveRenderer(renderer);
    }

    // Must equal sizeof(RenderContext::FrameDescriptor), see static_assert in RiveSharpInterop.cpp.
    private const int NativeFrameDescriptorSize = 72;

    public unsafe void BeginFrame(ref readonly FrameDescriptor frameDescriptor)
    {
        if (sizeof(FrameDescriptor) != NativeFrameDescriptorSize)
            throw new InvalidOperationException($"FrameDescriptor is {sizeof(FrameDescriptor)} bytes, native layout expects {NativeFrameDescriptorSize}.");
        rive_RenderContext_BeginFrame(handle, (FrameDescriptor*)Unsafe.AsPointer(ref Unsafe.AsRef(in frameDescriptor)));
    }

    // Resources last used on or before the safe frame number may be recycled by Rive.
    private const int FramesInFlight = 3;
    private ulong frameNumber;

    public void Flush(RiveRenderTarget renderTarget)
    {
        frameNumber++;
        var safeFrameNumber = frameNumber > FramesInFlight ? frameNumber - FramesInFlight : 0;
        rive_RenderContext_Flush(handle, renderTarget.DangerousGetHandle(), frameNumber, safeFrameNumber);
    }

    protected override bool ReleaseHandle()
    {
        rive_RenderContext_Destroy(handle);
        return true;
    }
}