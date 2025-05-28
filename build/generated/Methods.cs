using System.Runtime.InteropServices;

namespace RiveSharpInterop
{
    internal static unsafe partial class Methods
    {
        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::gpu::RenderContext *")]
        public static extern nint rive_RenderContext_Create_D3D11([NativeTypeName("ID3D11Device*")] nint device, [NativeTypeName("ID3D11DeviceContext*")] nint deviceContext);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::gpu::RenderTarget *")]
        public static extern nint rive_RenderContext_MakeRenderTarget_D3D11([NativeTypeName("rive::gpu::RenderContext *")] nint self, int width, int height);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_RenderContext_BeginFrame([NativeTypeName("rive::gpu::RenderContext *")] nint self, int renderTargetWidth, int renderTargetHeight, int msaaSampleCount);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_RenderContext_Flush([NativeTypeName("rive::gpu::RenderContext *")] nint self, [NativeTypeName("rive::gpu::RenderTarget *")] nint renderTarget);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_RenderContext_Destroy([NativeTypeName("rive::gpu::RenderContext *")] nint self);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_RenderTarget_D3D11_SetTargetTexture([NativeTypeName("rive::gpu::RenderTarget *")] nint self, [NativeTypeName("ID3D11Texture2D*")] nint texture);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_RenderTarget_Destroy([NativeTypeName("rive::gpu::RenderTarget *")] nint self);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::Renderer *")]
        public static extern nint rive_Renderer_Create([NativeTypeName("rive::gpu::RenderContext *")] nint renderContext);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Renderer_Destroy([NativeTypeName("rive::Renderer *")] nint renderer);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::File *")]
        public static extern nint rive_File_Import([NativeTypeName("uint8_t *")] byte* data, int dataLength, [NativeTypeName("rive::Factory *")] nint factory);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_File_Destroy([NativeTypeName("rive::File *")] nint file);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ArtboardInstance *")]
        public static extern nint rive_File_GetArtboardDefault([NativeTypeName("rive::File *")] nint file);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::Scene *")]
        public static extern nint rive_ArtboardInstance_StaticScene([NativeTypeName("rive::ArtboardInstance *")] nint artboard);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::Scene *")]
        public static extern nint rive_ArtboardInstance_StateMachineAt([NativeTypeName("rive::ArtboardInstance *")] nint artboard, int index);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::Scene *")]
        public static extern nint rive_ArtboardInstance_AnimationAt([NativeTypeName("rive::ArtboardInstance *")] nint artboard, int index);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ArtboardInstance_Destroy([NativeTypeName("rive::ArtboardInstance *")] nint artboard);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Scene_AdvanceAndApply([NativeTypeName("rive::Scene *")] nint self, float seconds);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Scene_Draw([NativeTypeName("rive::Scene *")] nint scene, [NativeTypeName("rive::Renderer *")] nint renderer, int width, int height);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Scene_Destroy([NativeTypeName("rive::Scene *")] nint self);
    }
}
