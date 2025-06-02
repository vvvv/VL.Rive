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
        [return: NativeTypeName("rive::ViewModelRuntime *")]
        public static extern nint rive_File_DefaultArtboardViewModel([NativeTypeName("rive::File *")] nint file, [NativeTypeName("rive::Artboard *")] nint artboard);

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
        [return: NativeTypeName("rive::Scene *")]
        public static extern nint rive_ArtboardInstance_DefaultScene([NativeTypeName("rive::ArtboardInstance *")] nint artboard);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ArtboardInstance_Destroy([NativeTypeName("rive::ArtboardInstance *")] nint artboard);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Artboard_BindViewModelInstance([NativeTypeName("rive::Artboard *")] nint artboard, [NativeTypeName("rive::ViewModelInstance *")] nint viewModelInstance);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Scene_AdvanceAndApply([NativeTypeName("rive::Scene *")] nint self, float seconds);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Scene_Draw([NativeTypeName("rive::Scene *")] nint scene, [NativeTypeName("rive::Renderer *")] nint renderer, int width, int height);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Scene_Destroy([NativeTypeName("rive::Scene *")] nint self);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Scene_BindViewModelInstance([NativeTypeName("rive::Scene *")] nint scene, [NativeTypeName("rive::ViewModelInstance *")] nint viewModelInstance);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* rive_ViewModelRuntime_Name([NativeTypeName("rive::ViewModelRuntime *")] nint runtime);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceRuntime *")]
        public static extern nint rive_ViewModelRuntime_CreateInstance([NativeTypeName("rive::ViewModelRuntime *")] nint runtime);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* rive_ViewModelInstanceRuntime_Name([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint rive_ViewModelInstanceRuntime_PropertyCount([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceNumberRuntime *")]
        public static extern nint rive_ViewModelInstanceRuntime_PropertyNumber([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceStringRuntime *")]
        public static extern nint rive_ViewModelInstanceRuntime_PropertyString([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceBooleanRuntime *")]
        public static extern nint rive_ViewModelInstanceRuntime_PropertyBoolean([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceColorRuntime *")]
        public static extern nint rive_ViewModelInstanceRuntime_PropertyColor([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceEnumRuntime *")]
        public static extern nint rive_ViewModelInstanceRuntime_PropertyEnum([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceTriggerRuntime *")]
        public static extern nint rive_ViewModelInstanceRuntime_PropertyTrigger([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceListRuntime *")]
        public static extern nint rive_ViewModelInstanceRuntime_PropertyList([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceRuntime *")]
        public static extern nint rive_ViewModelInstanceRuntime_PropertyViewModel([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceAssetImageRuntime *")]
        public static extern nint rive_ViewModelInstanceRuntime_PropertyImage([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceValueRuntime *")]
        public static extern nint rive_ViewModelInstanceRuntime_Property([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceRuntime_Properties([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime, RivePropertyData* properties_out);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte rive_ViewModelInstanceValueRuntime_HasChanged([NativeTypeName("rive::ViewModelInstanceValueRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceValueRuntime_ClearChanges([NativeTypeName("rive::ViewModelInstanceValueRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern double rive_ViewModelInstanceNumberRuntime_Value([NativeTypeName("rive::ViewModelInstanceNumberRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceNumberRuntime_SetValue([NativeTypeName("rive::ViewModelInstanceNumberRuntime *")] nint value, double v);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* rive_ViewModelInstanceStringRuntime_Value([NativeTypeName("rive::ViewModelInstanceStringRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceStringRuntime_SetValue([NativeTypeName("rive::ViewModelInstanceStringRuntime *")] nint value, [NativeTypeName("const char *")] sbyte* v);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte rive_ViewModelInstanceBooleanRuntime_Value([NativeTypeName("rive::ViewModelInstanceBooleanRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceBooleanRuntime_SetValue([NativeTypeName("rive::ViewModelInstanceBooleanRuntime *")] nint value, [NativeTypeName("bool")] byte v);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint rive_ViewModelInstanceColorRuntime_Value([NativeTypeName("rive::ViewModelInstanceColorRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceColorRuntime_SetValue([NativeTypeName("rive::ViewModelInstanceColorRuntime *")] nint value, [NativeTypeName("uint32_t")] uint v);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint rive_ViewModelInstanceEnumRuntime_ValueIndex([NativeTypeName("rive::ViewModelInstanceEnumRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceEnumRuntime_SetValueIndex([NativeTypeName("rive::ViewModelInstanceEnumRuntime *")] nint value, [NativeTypeName("uint32_t")] uint v);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceTriggerRuntime_Trigger([NativeTypeName("rive::ViewModelInstanceTriggerRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint rive_ViewModelInstanceListRuntime_Size([NativeTypeName("rive::ViewModelInstanceListRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceRuntime *")]
        public static extern nint rive_ViewModelInstanceListRuntime_At([NativeTypeName("rive::ViewModelInstanceListRuntime *")] nint value, int index);
    }
}
