using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static RiveSharpInterop.Methods;

namespace VL.Rive
{
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

    internal class RiveRenderContextD3D11 : RiveRenderContext
    {
        public static RiveRenderContextD3D11 Create(nint device, nint deviceContext)
        {
            var handle = rive_RenderContext_Create_D3D11(device, deviceContext);
            if (handle == nint.Zero)
            {
                throw new InvalidOperationException("Failed to create RiveRenderContext for D3D11.");
            }
            return new RiveRenderContextD3D11(handle);
        }

        public RiveRenderContextD3D11(nint handle) : base(handle)
        {
        }

        public RiveRenderTargetD3D11 MakeRenderTarget(int width, int height)
        {
            var handle = rive_RenderContext_MakeRenderTarget_D3D11(this.handle, width, height);
            if (handle == nint.Zero)
            {
                throw new InvalidOperationException("Failed to create RiveRenderTarget for D3D11.");
            }
            return new RiveRenderTargetD3D11(handle);
        }
    }
}