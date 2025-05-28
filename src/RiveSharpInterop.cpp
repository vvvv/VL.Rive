#include "RiveSharpInterop.hpp"
#include "rive/static_scene.hpp"

using namespace rive;
using namespace rive::gpu;

extern "C"
{
    // RenderContext
    __declspec(dllexport) RenderContext* rive_RenderContext_Create_D3D11(ID3D11Device* device, ID3D11DeviceContext* deviceContext)
    {
        D3DContextOptions options;
        return RenderContextD3DImpl::MakeContext(device, deviceContext, options).release();
    }

    __declspec(dllexport) RenderTarget* rive_RenderContext_MakeRenderTarget_D3D11(RenderContext* self, int width, int height)
    {
        auto d3dImpl = static_cast<RenderContextD3DImpl*>(self->impl());
        return d3dImpl->makeRenderTarget(width, height).release();
    }

    __declspec(dllexport) void rive_RenderContext_BeginFrame(RenderContext* self, int renderTargetWidth, int renderTargetHeight, int msaaSampleCount)
    {
		self->beginFrame({
            .renderTargetWidth = static_cast<uint32_t>(renderTargetWidth),
            .renderTargetHeight = static_cast<uint32_t>(renderTargetHeight),
            .loadAction = LoadAction::preserveRenderTarget,
            .msaaSampleCount = msaaSampleCount,
        });
    }

    __declspec(dllexport) void rive_RenderContext_Flush(RenderContext* self, RenderTarget* renderTarget)
    {
        self->flush({
			.renderTarget = renderTarget,
        });
    }

    __declspec(dllexport) void rive_RenderContext_Destroy(RenderContext* self)
    {
        delete self;
    }

    // RenderTarget
    __declspec(dllexport) void rive_RenderTarget_D3D11_SetTargetTexture(RenderTarget* self, ID3D11Texture2D* texture)
    {
        auto d3dTarget = static_cast<RenderTargetD3D*>(self);
        d3dTarget->setTargetTexture(texture);
    }

    __declspec(dllexport) void rive_RenderTarget_Destroy(RenderTarget* self)
    {
        delete self;
    }

    // Renderer
    __declspec(dllexport) Renderer* rive_Renderer_Create(RenderContext* renderContext)
    {
        return new RiveRenderer(renderContext);
    }

    __declspec(dllexport) void rive_Renderer_Destroy(Renderer* renderer)
    {
        delete renderer;
    }

    // File
    __declspec(dllexport) File* rive_File_Import(uint8_t* data, int dataLength, Factory* factory)
    {
        auto file = File::import(rive::Span<const uint8_t>(data, dataLength), factory);
        return file ? file.release() : nullptr;
    }

    __declspec(dllexport) void rive_File_Destroy(File* file)
    {
        delete file;
    }

    __declspec(dllexport) ArtboardInstance* rive_File_GetArtboardDefault(File* file)
    {
        return file->artboardDefault().release();
    }

    // Artboard
    __declspec(dllexport) Scene* rive_ArtboardInstance_StaticScene(ArtboardInstance* artboard)
    {
        return new StaticScene(artboard);
    }

    __declspec(dllexport) Scene* rive_ArtboardInstance_StateMachineAt(ArtboardInstance* artboard, int index)
    {
        return artboard->stateMachineAt(index).release();
    }

    __declspec(dllexport) Scene* rive_ArtboardInstance_AnimationAt(ArtboardInstance* artboard, int index)
    {
        return artboard->animationAt(index).release();
    }

    __declspec(dllexport) void rive_ArtboardInstance_Destroy(ArtboardInstance* artboard)
    {
        delete artboard;
    }

    // Scene
    __declspec(dllexport) void rive_Scene_AdvanceAndApply(Scene* self, float seconds)
    {
		self->advanceAndApply(seconds);
    }

    __declspec(dllexport) void rive_Scene_Draw(Scene* scene, Renderer* renderer)
    {
        scene->draw(renderer);
    }

    __declspec(dllexport) void rive_Scene_Destroy(Scene* self)
    {
		delete self;
    }
}