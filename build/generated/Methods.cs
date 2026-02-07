using System.Runtime.InteropServices;

namespace VL.Rive.Interop
{
    internal static unsafe partial class Methods
    {
        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern RiveMat2D rive_ComputeAlignment(RiveFit fit, RiveAlignment alignment, RiveAABB frame, RiveAABB content, float scaleFactor);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern RiveMat2D rive_Mat2D_InvertOrIdentity([NativeTypeName("const RiveMat2D *")] RiveMat2D* inMat);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::Factory *")]
        public static extern nint rive_Factory_Create();

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Factory_Destroy([NativeTypeName("rive::Factory *")] nint factory);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::RenderImage *")]
        public static extern nint rive_Factory_DecodeImage([NativeTypeName("rive::Factory *")] nint factory, [NativeTypeName("uint8_t *")] byte* data, int dataLength);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::Font *")]
        public static extern nint rive_Factory_DecodeFont([NativeTypeName("rive::Factory *")] nint factory, [NativeTypeName("uint8_t *")] byte* data, int dataLength);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::AudioSource *")]
        public static extern nint rive_Factory_DecodeAudio([NativeTypeName("rive::Factory *")] nint factory, [NativeTypeName("uint8_t *")] byte* data, int dataLength);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_RenderImage_Release([NativeTypeName("rive::RenderImage *")] nint image);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Font_Release([NativeTypeName("rive::Font *")] nint font);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_AudioSource_Release([NativeTypeName("rive::AudioSource *")] nint audioSource);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_BindableArtboard_Release([NativeTypeName("rive::BindableArtboard *")] nint artboard);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::gpu::RenderContext *")]
        public static extern nint rive_RenderContext_Create_D3D11([NativeTypeName("ID3D11Device*")] nint device, [NativeTypeName("ID3D11DeviceContext*")] nint deviceContext);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::gpu::RenderTarget *")]
        public static extern nint rive_RenderContext_MakeRenderTarget_D3D11([NativeTypeName("rive::gpu::RenderContext *")] nint self, int width, int height);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_RenderContext_BeginFrame([NativeTypeName("rive::gpu::RenderContext *")] nint self, [NativeTypeName("const RenderContext::FrameDescriptor *")] FrameDescriptor* frameDesc);

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
        public static extern void rive_Renderer_Save([NativeTypeName("rive::Renderer *")] nint renderer);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Renderer_Restore([NativeTypeName("rive::Renderer *")] nint renderer);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Renderer_Transform([NativeTypeName("rive::Renderer *")] nint renderer, [NativeTypeName("const RiveMat2D *")] RiveMat2D* mat);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::File *")]
        public static extern nint rive_File_Import([NativeTypeName("uint8_t *")] byte* data, int dataLength, [NativeTypeName("rive::Factory *")] nint factory, RiveImportResult* result);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_File_Destroy([NativeTypeName("rive::File *")] nint file);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int rive_File_ArtboardCount([NativeTypeName("rive::File *")] nint file);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_File_Artboards([NativeTypeName("rive::File *")] nint file, [NativeTypeName("Artboard **")] nint* artboards_out);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ArtboardInstance *")]
        public static extern nint rive_File_ArtboardByName([NativeTypeName("rive::File *")] nint file, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ArtboardInstance *")]
        public static extern nint rive_File_GetArtboardDefault([NativeTypeName("rive::File *")] nint file);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::BindableArtboard *")]
        public static extern nint rive_File_BindableArtboardNamed([NativeTypeName("rive::File *")] nint file, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::BindableArtboard *")]
        public static extern nint rive_File_BindableArtboardDefault([NativeTypeName("rive::File *")] nint file);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelRuntime *")]
        public static extern nint rive_File_DefaultArtboardViewModel([NativeTypeName("rive::File *")] nint file, [NativeTypeName("rive::Artboard *")] nint artboard);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int rive_File_ViewModelCount([NativeTypeName("rive::File *")] nint file);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_File_GetViewModel([NativeTypeName("rive::File *")] nint file, int index, [NativeTypeName("char **")] sbyte** name_out, int* propertiesCount_out);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_File_GetViewModelProperties([NativeTypeName("rive::File *")] nint file, int index, RivePropertyData* properties_out);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelRuntime *")]
        public static extern nint rive_File_ViewModelByIndex([NativeTypeName("rive::File *")] nint file, int index);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelRuntime *")]
        public static extern nint rive_File_ViewModelByName([NativeTypeName("rive::File *")] nint file, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* rive_Artboard_Name([NativeTypeName("rive::Artboard *")] nint artboard);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int rive_Artboard_StateMachineCount([NativeTypeName("rive::Artboard *")] nint artboard);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Artboard_StateMachines([NativeTypeName("rive::Artboard *")] nint artboard, [NativeTypeName("StateMachine **")] nint* stateMachines_out);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int rive_Artboard_AnimationCount([NativeTypeName("rive::Artboard *")] nint artboard);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Artboard_Animations([NativeTypeName("rive::Artboard *")] nint artboard, [NativeTypeName("Animation **")] nint* animations_out);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern RiveAABB rive_Artboard_Bounds([NativeTypeName("rive::Artboard *")] nint artboard);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint rive_Artboard_ViewModelId([NativeTypeName("rive::Artboard *")] nint artboard);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* rive_StateMachine_Name([NativeTypeName("rive::StateMachine *")] nint stateMachine);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* rive_Animation_Name([NativeTypeName("rive::Animation *")] nint animation);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::Scene *")]
        public static extern nint rive_ArtboardInstance_SceneByName([NativeTypeName("rive::ArtboardInstance *")] nint artboard, [NativeTypeName("const char *")] sbyte* name);

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
        public static extern void rive_Scene_Draw([NativeTypeName("rive::Scene *")] nint scene, [NativeTypeName("rive::Renderer *")] nint renderer);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Scene_Destroy([NativeTypeName("rive::Scene *")] nint self);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float rive_Scene_Width([NativeTypeName("rive::Scene *")] nint scene);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float rive_Scene_Height([NativeTypeName("rive::Scene *")] nint scene);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern RiveAABB rive_Scene_Bounds([NativeTypeName("rive::Scene *")] nint scene);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* rive_Scene_Name([NativeTypeName("rive::Scene *")] nint scene);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int rive_Scene_Loop([NativeTypeName("rive::Scene *")] nint scene);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte rive_Scene_IsTranslucent([NativeTypeName("rive::Scene *")] nint scene);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float rive_Scene_DurationSeconds([NativeTypeName("rive::Scene *")] nint scene);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte rive_Scene_AdvanceAndApply([NativeTypeName("rive::Scene *")] nint scene, float elapsedSeconds);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_Scene_BindViewModelInstance([NativeTypeName("rive::Scene *")] nint scene, [NativeTypeName("rive::ViewModelInstance *")] nint viewModelInstance);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern RiveHitResult rive_Scene_PointerDown([NativeTypeName("rive::Scene *")] nint scene, float x, float y, int pointerId);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern RiveHitResult rive_Scene_PointerMove([NativeTypeName("rive::Scene *")] nint scene, float x, float y, float timeStamp, int pointerId);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern RiveHitResult rive_Scene_PointerUp([NativeTypeName("rive::Scene *")] nint scene, float x, float y, int pointerId);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern RiveHitResult rive_Scene_PointerExit([NativeTypeName("rive::Scene *")] nint scene, float x, float y, int pointerId);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint rive_Scene_InputCount([NativeTypeName("rive::Scene *")] nint scene);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::SMIInput *")]
        public static extern nint rive_Scene_Input([NativeTypeName("rive::Scene *")] nint scene, [NativeTypeName("size_t")] nuint index);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::SMIBool *")]
        public static extern nint rive_Scene_GetBool([NativeTypeName("rive::Scene *")] nint scene, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::SMINumber *")]
        public static extern nint rive_Scene_GetNumber([NativeTypeName("rive::Scene *")] nint scene, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::SMITrigger *")]
        public static extern nint rive_Scene_GetTrigger([NativeTypeName("rive::Scene *")] nint scene, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* rive_ViewModelRuntime_Name([NativeTypeName("rive::ViewModelRuntime *")] nint runtime);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceRuntime *")]
        public static extern nint rive_ViewModelRuntime_CreateInstance([NativeTypeName("rive::ViewModelRuntime *")] nint runtime);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceRuntime *")]
        public static extern nint rive_ViewModelRuntime_CreateDefaultInstance([NativeTypeName("rive::ViewModelRuntime *")] nint runtime);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* rive_ViewModelInstanceRuntime_Name([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int rive_ViewModelInstanceRuntime_PropertyCount([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime);

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
        [return: NativeTypeName("rive::ViewModelInstanceArtboardRuntime *")]
        public static extern nint rive_ViewModelInstanceRuntime_PropertyArtboard([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceValueRuntime *")]
        public static extern nint rive_ViewModelInstanceRuntime_Property([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceRuntime_Properties([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime, RivePropertyData* properties_out);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstance *")]
        public static extern nint rive_ViewModelInstanceRuntime_Instance([NativeTypeName("rive::ViewModelInstanceRuntime *")] nint runtime);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint rive_ViewModelInstance_ViewModelId([NativeTypeName("rive::ViewModelInstance *")] nint instance);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte rive_ViewModelInstanceValueRuntime_HasChanged([NativeTypeName("rive::ViewModelInstanceValueRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceValueRuntime_ClearChanges([NativeTypeName("rive::ViewModelInstanceValueRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern RiveDataType rive_ViewModelInstanceValueRuntime_DataType([NativeTypeName("rive::ViewModelInstanceValueRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float rive_ViewModelInstanceNumberRuntime_Value([NativeTypeName("rive::ViewModelInstanceNumberRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceNumberRuntime_SetValue([NativeTypeName("rive::ViewModelInstanceNumberRuntime *")] nint value, float v);

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
        [return: NativeTypeName("const char *")]
        public static extern sbyte* rive_ViewModelInstanceEnumRuntime_Value([NativeTypeName("rive::ViewModelInstanceEnumRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceEnumRuntime_SetValue([NativeTypeName("rive::ViewModelInstanceEnumRuntime *")] nint value, [NativeTypeName("const char *")] sbyte* v);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint rive_ViewModelInstanceEnumRuntime_ValueIndex([NativeTypeName("rive::ViewModelInstanceEnumRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceEnumRuntime_SetValueIndex([NativeTypeName("rive::ViewModelInstanceEnumRuntime *")] nint value, [NativeTypeName("uint32_t")] uint v);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint rive_ViewModelInstanceEnumRuntime_ValuesCount([NativeTypeName("rive::ViewModelInstanceEnumRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* rive_ViewModelInstanceEnumRuntime_ValueAt([NativeTypeName("rive::ViewModelInstanceEnumRuntime *")] nint value, [NativeTypeName("size_t")] nuint index);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* rive_ViewModelInstanceEnumRuntime_EnumType([NativeTypeName("rive::ViewModelInstanceEnumRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceTriggerRuntime_Trigger([NativeTypeName("rive::ViewModelInstanceTriggerRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint rive_ViewModelInstanceListRuntime_Size([NativeTypeName("rive::ViewModelInstanceListRuntime *")] nint value);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("rive::ViewModelInstanceRuntime *")]
        public static extern nint rive_ViewModelInstanceListRuntime_At([NativeTypeName("rive::ViewModelInstanceListRuntime *")] nint value, int index);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceListRuntime_AddInstance([NativeTypeName("rive::ViewModelInstanceListRuntime *")] nint value, [NativeTypeName("rive::ViewModelInstanceRuntime *")] nint instance);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte rive_ViewModelInstanceListRuntime_AddInstanceAt([NativeTypeName("rive::ViewModelInstanceListRuntime *")] nint value, [NativeTypeName("rive::ViewModelInstanceRuntime *")] nint instance, int index);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceListRuntime_RemoveInstance([NativeTypeName("rive::ViewModelInstanceListRuntime *")] nint value, [NativeTypeName("rive::ViewModelInstanceRuntime *")] nint instance);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceListRuntime_RemoveInstanceAt([NativeTypeName("rive::ViewModelInstanceListRuntime *")] nint value, int index);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceListRuntime_Swap([NativeTypeName("rive::ViewModelInstanceListRuntime *")] nint value, [NativeTypeName("uint32_t")] uint index1, [NativeTypeName("uint32_t")] uint index2);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceAssetImageRuntime_SetValue([NativeTypeName("rive::ViewModelInstanceAssetImageRuntime *")] nint value, [NativeTypeName("rive::RenderImage *")] nint renderImage);

        [DllImport("rive_interop", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void rive_ViewModelInstanceArtboardRuntime_SetValue([NativeTypeName("rive::ViewModelInstanceArtboardRuntime *")] nint value, [NativeTypeName("rive::BindableArtboard *")] nint bindableArtboard);
    }
}
