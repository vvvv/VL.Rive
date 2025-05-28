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

	// Artboard
	__declspec(dllexport) Scene* rive_ArtboardInstance_StaticScene(ArtboardInstance* artboard);
	__declspec(dllexport) Scene* rive_ArtboardInstance_StateMachineAt(ArtboardInstance* artboard, int index);
	__declspec(dllexport) Scene* rive_ArtboardInstance_AnimationAt(ArtboardInstance* artboard, int index);
	__declspec(dllexport) void rive_ArtboardInstance_Destroy(ArtboardInstance* artboard);

	// Scene
	__declspec(dllexport) void rive_Scene_AdvanceAndApply(Scene* self, float seconds);
	__declspec(dllexport) void rive_Scene_Draw(Scene* scene, Renderer* renderer, int width, int height);
	__declspec(dllexport) void rive_Scene_Destroy(Scene* self);
}
