#include "rive/animation/state_machine_input_instance.hpp"
#include "rive/factory.hpp"
#include "utils/factory_utils.hpp"
#include "rive/file.hpp"
#include "rive/animation/animation.hpp"
#include "rive/animation/linear_animation_instance.hpp"
#include "rive/animation/linear_animation.hpp"
#include "rive/animation/state_machine_instance.hpp"
#include "rive/artboard.hpp"
#include "rive/renderer.hpp"
#include "rive/renderer/render_context.hpp"
#include "rive/renderer/rive_renderer.hpp"
#include "rive/renderer/d3d11/render_context_d3d_impl.hpp"
#include "rive/renderer/d3d11/d3d11.hpp"
#include "rive/viewmodel/runtime/viewmodel_runtime.hpp"

using namespace rive;
using namespace rive::gpu;

extern "C"
{
	// RenderContext
    __declspec(dllexport) RenderContext* rive_RenderContext_Create_D3D11(ID3D11Device* device, ID3D11DeviceContext* deviceContext);
	__declspec(dllexport) RenderTarget* rive_RenderContext_MakeRenderTarget_D3D11(RenderContext* self, int width, int height);
	__declspec(dllexport) void rive_RenderContext_BeginFrame(RenderContext* self, int renderTargetWidth, int renderTargetHeight, int msaaSampleCount);
	__declspec(dllexport) void rive_RenderContext_Flush(RenderContext* self, RenderTarget* renderTarget);
	__declspec(dllexport) void rive_RenderContext_Destroy(RenderContext* self);

	// RenderTarget
	__declspec(dllexport) void rive_RenderTarget_D3D11_SetTargetTexture(RenderTarget* self, ID3D11Texture2D* texture);
	__declspec(dllexport) void rive_RenderTarget_Destroy(RenderTarget* self);

	// Renderer
	__declspec(dllexport) Renderer* rive_Renderer_Create(RenderContext* renderContext);
	__declspec(dllexport) void rive_Renderer_Destroy(Renderer* renderer);

	// File
	__declspec(dllexport) File* rive_File_Import(uint8_t* data, int dataLength, Factory* factory);
	__declspec(dllexport) void rive_File_Destroy(File* file);
	__declspec(dllexport) ArtboardInstance* rive_File_GetArtboardDefault(File* file);
	__declspec(dllexport) ViewModelRuntime* rive_File_DefaultArtboardViewModel(File* file, Artboard* artboard);

	// Artboard
	__declspec(dllexport) Scene* rive_ArtboardInstance_StaticScene(ArtboardInstance* artboard);
	__declspec(dllexport) Scene* rive_ArtboardInstance_StateMachineAt(ArtboardInstance* artboard, int index);
	__declspec(dllexport) Scene* rive_ArtboardInstance_AnimationAt(ArtboardInstance* artboard, int index);
	__declspec(dllexport) Scene* rive_ArtboardInstance_DefaultScene(ArtboardInstance* artboard);
	__declspec(dllexport) void rive_ArtboardInstance_Destroy(ArtboardInstance* artboard);

    __declspec(dllexport) void rive_Artboard_BindViewModelInstance(Artboard* artboard, ViewModelInstance* viewModelInstance);

	// Scene
	__declspec(dllexport) void rive_Scene_AdvanceAndApply(Scene* self, float seconds);
	__declspec(dllexport) void rive_Scene_Draw(Scene* scene, Renderer* renderer, int width, int height);
	__declspec(dllexport) void rive_Scene_Destroy(Scene* self);
	__declspec(dllexport) void rive_Scene_BindViewModelInstance(Scene* scene, ViewModelInstance* viewModelInstance);

	// ViewModelRuntime
	// Returns the name of the ViewModelRuntime (pointer valid as long as runtime is alive)
	__declspec(dllexport) const char* rive_ViewModelRuntime_Name(ViewModelRuntime* runtime);

	// Creates a new ViewModelInstanceRuntime from the ViewModelRuntime
	__declspec(dllexport) ViewModelInstanceRuntime* rive_ViewModelRuntime_CreateInstance(ViewModelRuntime* runtime);

	// Property data struct for C API
	struct RivePropertyData
	{
		int type;
		const char* name;
	};

	// ViewModelInstanceRuntime C-API
	// Returns the name of the ViewModelInstanceRuntime (pointer valid as long as the object is alive)
	__declspec(dllexport) const char* rive_ViewModelInstanceRuntime_Name(ViewModelInstanceRuntime* runtime);

	// Returns the number of properties in the ViewModelInstanceRuntime
	__declspec(dllexport) size_t rive_ViewModelInstanceRuntime_PropertyCount(ViewModelInstanceRuntime* runtime);

	// Returns a property as a number runtime by path (returns nullptr if not found or not a number)
	__declspec(dllexport) ViewModelInstanceNumberRuntime* rive_ViewModelInstanceRuntime_PropertyNumber(ViewModelInstanceRuntime* runtime, const char* path);

	// Returns a property as a string runtime by path (returns nullptr if not found or not a string)
	__declspec(dllexport) ViewModelInstanceStringRuntime* rive_ViewModelInstanceRuntime_PropertyString(ViewModelInstanceRuntime* runtime, const char* path);

	// Returns a property as a boolean runtime by path (returns nullptr if not found or not a boolean)
	__declspec(dllexport) ViewModelInstanceBooleanRuntime* rive_ViewModelInstanceRuntime_PropertyBoolean(ViewModelInstanceRuntime* runtime, const char* path);

	// Returns a property as a color runtime by path (returns nullptr if not found or not a color)
	__declspec(dllexport) ViewModelInstanceColorRuntime* rive_ViewModelInstanceRuntime_PropertyColor(ViewModelInstanceRuntime* runtime, const char* path);

	// Returns a property as an enum runtime by path (returns nullptr if not found or not an enum)
	__declspec(dllexport) ViewModelInstanceEnumRuntime* rive_ViewModelInstanceRuntime_PropertyEnum(ViewModelInstanceRuntime* runtime, const char* path);

	// Returns a property as a trigger runtime by path (returns nullptr if not found or not a trigger)
	__declspec(dllexport) ViewModelInstanceTriggerRuntime* rive_ViewModelInstanceRuntime_PropertyTrigger(ViewModelInstanceRuntime* runtime, const char* path);

	// Returns a property as a list runtime by path (returns nullptr if not found or not a list)
	__declspec(dllexport) ViewModelInstanceListRuntime* rive_ViewModelInstanceRuntime_PropertyList(ViewModelInstanceRuntime* runtime, const char* path);

	// Returns a property as a nested ViewModelInstanceRuntime by path (returns nullptr if not found or not a viewmodel)
	__declspec(dllexport) ViewModelInstanceRuntime* rive_ViewModelInstanceRuntime_PropertyViewModel(ViewModelInstanceRuntime* runtime, const char* path);

	// Returns a property as an asset image runtime by path (returns nullptr if not found or not an image)
	__declspec(dllexport) ViewModelInstanceAssetImageRuntime* rive_ViewModelInstanceRuntime_PropertyImage(ViewModelInstanceRuntime* runtime, const char* path);

	// Returns the generic property value runtime by path (returns nullptr if not found)
	__declspec(dllexport) ViewModelInstanceValueRuntime* rive_ViewModelInstanceRuntime_Property(ViewModelInstanceRuntime* runtime, const char* path);

	// Returns the array of property metadata (see RivePropertyData, same as ViewModelRuntime)
	__declspec(dllexport) void rive_ViewModelInstanceRuntime_Properties(ViewModelInstanceRuntime* runtime, RivePropertyData* properties_out);

	// ViewModelInstanceValueRuntime
	__declspec(dllexport) bool rive_ViewModelInstanceValueRuntime_HasChanged(ViewModelInstanceValueRuntime* value);
	__declspec(dllexport) void rive_ViewModelInstanceValueRuntime_ClearChanges(ViewModelInstanceValueRuntime* value);

	// ViewModelInstanceNumberRuntime
	__declspec(dllexport) double rive_ViewModelInstanceNumberRuntime_Value(ViewModelInstanceNumberRuntime* value);
	__declspec(dllexport) void rive_ViewModelInstanceNumberRuntime_SetValue(ViewModelInstanceNumberRuntime* value, double v);

	// ViewModelInstanceStringRuntime
	__declspec(dllexport) const char* rive_ViewModelInstanceStringRuntime_Value(ViewModelInstanceStringRuntime* value);
	__declspec(dllexport) void rive_ViewModelInstanceStringRuntime_SetValue(ViewModelInstanceStringRuntime* value, const char* v);

	// ViewModelInstanceBooleanRuntime
	__declspec(dllexport) bool rive_ViewModelInstanceBooleanRuntime_Value(ViewModelInstanceBooleanRuntime* value);
	__declspec(dllexport) void rive_ViewModelInstanceBooleanRuntime_SetValue(ViewModelInstanceBooleanRuntime* value, bool v);

	// ViewModelInstanceColorRuntime
	__declspec(dllexport) uint32_t rive_ViewModelInstanceColorRuntime_Value(ViewModelInstanceColorRuntime* value);
	__declspec(dllexport) void rive_ViewModelInstanceColorRuntime_SetValue(ViewModelInstanceColorRuntime* value, uint32_t v);

	// ViewModelInstanceEnumRuntime
	__declspec(dllexport) uint32_t rive_ViewModelInstanceEnumRuntime_ValueIndex(ViewModelInstanceEnumRuntime* value);
	__declspec(dllexport) void rive_ViewModelInstanceEnumRuntime_SetValueIndex(ViewModelInstanceEnumRuntime* value, uint32_t v);

	// ViewModelInstanceTriggerRuntime
	__declspec(dllexport) void rive_ViewModelInstanceTriggerRuntime_Trigger(ViewModelInstanceTriggerRuntime* value);

	// ViewModelInstanceListRuntime
	__declspec(dllexport) size_t rive_ViewModelInstanceListRuntime_Size(ViewModelInstanceListRuntime* value);
	__declspec(dllexport) ViewModelInstanceRuntime* rive_ViewModelInstanceListRuntime_At(ViewModelInstanceListRuntime* value, int index);
}