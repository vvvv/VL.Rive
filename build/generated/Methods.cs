using System.Runtime.InteropServices;

namespace RiveSharpInterop
{
    internal static partial class Methods
    {
        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::gpu::RenderContext *")]
        public static extern nint CreateRenderContextD3D11([NativeTypeName("ID3D11Device*")] nint device, [NativeTypeName("ID3D11DeviceContext*")] nint deviceContext);
    }
}
