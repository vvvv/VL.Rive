#include "RiveSharpInterop.hpp"
#include "rive/static_scene.hpp"

using namespace rive;
using namespace rive::gpu;

extern "C"
{
    // RenderContext
    RenderContext* rive_RenderContext_Create_D3D11(ID3D11Device* device, ID3D11DeviceContext* deviceContext)
    {
        D3DContextOptions options;
        return RenderContextD3DImpl::MakeContext(device, deviceContext, options).release();
    }

    RenderTarget* rive_RenderContext_MakeRenderTarget_D3D11(RenderContext* self, int width, int height)
    {
        auto d3dImpl = static_cast<RenderContextD3DImpl*>(self->impl());
        return d3dImpl->makeRenderTarget(width, height).release();
    }

    void rive_RenderContext_BeginFrame(RenderContext* self, int renderTargetWidth, int renderTargetHeight, int msaaSampleCount)
    {
		self->beginFrame({
            .renderTargetWidth = static_cast<uint32_t>(renderTargetWidth),
            .renderTargetHeight = static_cast<uint32_t>(renderTargetHeight),
            .loadAction = LoadAction::preserveRenderTarget,
            .msaaSampleCount = msaaSampleCount,
        });
    }

    void rive_RenderContext_Flush(RenderContext* self, RenderTarget* renderTarget)
    {
        self->flush({
			.renderTarget = renderTarget,
        });
    }

    void rive_RenderContext_Destroy(RenderContext* self)
    {
        delete self;
    }

    // RenderTarget
    void rive_RenderTarget_D3D11_SetTargetTexture(RenderTarget* self, ID3D11Texture2D* texture)
    {
        auto d3dTarget = static_cast<RenderTargetD3D*>(self);
        d3dTarget->setTargetTexture(texture);
    }

    void rive_RenderTarget_Destroy(RenderTarget* self)
    {
        delete self;
    }

    // Renderer
    Renderer* rive_Renderer_Create(RenderContext* renderContext)
    {
        return new RiveRenderer(renderContext);
    }

    void rive_Renderer_Destroy(Renderer* renderer)
    {
        delete renderer;
    }

    // File
    File* rive_File_Import(uint8_t* data, int dataLength, Factory* factory)
    {
        auto file = File::import(rive::Span<const uint8_t>(data, dataLength), factory);
        return file ? file.release() : nullptr;
    }

    void rive_File_Destroy(File* file)
    {
        delete file;
    }

    ArtboardInstance* rive_File_GetArtboardDefault(File* file)
    {
        return file->artboardDefault().release();
    }

    // Artboard
    Scene* rive_ArtboardInstance_StaticScene(ArtboardInstance* artboard)
    {
        return new StaticScene(artboard);
    }

    Scene* rive_ArtboardInstance_StateMachineAt(ArtboardInstance* artboard, int index)
    {
        return artboard->stateMachineAt(index).release();
    }

    Scene* rive_ArtboardInstance_AnimationAt(ArtboardInstance* artboard, int index)
    {
        return artboard->animationAt(index).release();
    }

    Scene* rive_ArtboardInstance_DefaultScene(ArtboardInstance* artboard)
    {
        return artboard->defaultScene().release();
    }

    void rive_ArtboardInstance_Destroy(ArtboardInstance* artboard)
    {
        delete artboard;
    }

    void rive_Artboard_BindViewModelInstance(Artboard* artboard, ViewModelInstance* viewModelInstance)
    {
        // TODO: Needed?
        viewModelInstance->ref();
        artboard->bindViewModelInstance(rcp<ViewModelInstance>(viewModelInstance));
    }

    // Scene
    void rive_Scene_AdvanceAndApply(Scene* self, float seconds)
    {
		self->advanceAndApply(seconds);
    }

    void rive_Scene_Draw(Scene* scene, Renderer* renderer, int width, int height)
    {
        Mat2D m = computeAlignment(rive::Fit::contain,
            rive::Alignment::center,
            rive::AABB(0, 0, width, height),
            scene->bounds());
        renderer->save();
		renderer->transform(m);
        scene->draw(renderer);
		renderer->restore();
    }

    void rive_Scene_BindViewModelInstance(Scene* scene, ViewModelInstance* viewModelInstance)
    {
        // TODO: Needed?
        viewModelInstance->ref();
        scene->bindViewModelInstance(rcp<ViewModelInstance>(viewModelInstance));
    }

    void rive_Scene_Destroy(Scene* self)
    {
		delete self;
    }
}