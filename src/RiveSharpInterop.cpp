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

    ViewModelRuntime* rive_File_DefaultArtboardViewModel(File* file, Artboard* artboard)
    {
        return file->defaultArtboardViewModel(artboard);
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

    const char* rive_ViewModelRuntime_Name(ViewModelRuntime* runtime)
    {
        // Return a pointer to the internal string data (lifetime: as long as runtime is alive)
        return runtime->name().c_str();
    }

    ViewModelInstanceRuntime* rive_ViewModelRuntime_CreateInstance(ViewModelRuntime* runtime)
    {
        return runtime->createInstance();
    }

    const char* rive_ViewModelInstanceRuntime_Name(ViewModelInstanceRuntime* runtime)
    {
        return runtime->name().c_str();
    }

    size_t rive_ViewModelInstanceRuntime_PropertyCount(ViewModelInstanceRuntime* runtime)
    {
        return runtime->propertyCount();
    }

    ViewModelInstanceNumberRuntime* rive_ViewModelInstanceRuntime_PropertyNumber(ViewModelInstanceRuntime* runtime, const char* path)
    {
        return runtime->propertyNumber(std::string(path));
    }

    ViewModelInstanceStringRuntime* rive_ViewModelInstanceRuntime_PropertyString(ViewModelInstanceRuntime* runtime, const char* path)
    {
        return runtime->propertyString(std::string(path));
    }

    ViewModelInstanceBooleanRuntime* rive_ViewModelInstanceRuntime_PropertyBoolean(ViewModelInstanceRuntime* runtime, const char* path)
    {
        return runtime->propertyBoolean(std::string(path));
    }

    ViewModelInstanceColorRuntime* rive_ViewModelInstanceRuntime_PropertyColor(ViewModelInstanceRuntime* runtime, const char* path)
    {
        return runtime->propertyColor(std::string(path));
    }

    ViewModelInstanceEnumRuntime* rive_ViewModelInstanceRuntime_PropertyEnum(ViewModelInstanceRuntime* runtime, const char* path)
    {
        return runtime->propertyEnum(std::string(path));
    }

    ViewModelInstanceTriggerRuntime* rive_ViewModelInstanceRuntime_PropertyTrigger(ViewModelInstanceRuntime* runtime, const char* path)
    {
        return runtime->propertyTrigger(std::string(path));
    }

    ViewModelInstanceListRuntime* rive_ViewModelInstanceRuntime_PropertyList(ViewModelInstanceRuntime* runtime, const char* path)
    {
        return runtime->propertyList(std::string(path));
    }

    ViewModelInstanceRuntime* rive_ViewModelInstanceRuntime_PropertyViewModel(ViewModelInstanceRuntime* runtime, const char* path)
    {
        return runtime->propertyViewModel(std::string(path));
    }

    ViewModelInstanceAssetImageRuntime* rive_ViewModelInstanceRuntime_PropertyImage(ViewModelInstanceRuntime* runtime, const char* path)
    {
        return runtime->propertyImage(std::string(path));
    }

    ViewModelInstanceValueRuntime* rive_ViewModelInstanceRuntime_Property(ViewModelInstanceRuntime* runtime, const char* path)
    {
        return runtime->property(std::string(path));
    }

    void rive_ViewModelInstanceRuntime_Properties(ViewModelInstanceRuntime* runtime, RivePropertyData* properties_out)
    {
        std::vector<rive::PropertyData> props = runtime->properties();
        size_t count = props.size();
        for (size_t i = 0; i < count; ++i)
        {
            properties_out[i].type = static_cast<int>(props[i].type);
            properties_out[i].name = props[i].name.c_str();
        }
    }

    bool rive_ViewModelInstanceValueRuntime_HasChanged(ViewModelInstanceValueRuntime* value)
    {
        return value->hasChanged();
    }

    void rive_ViewModelInstanceValueRuntime_ClearChanges(ViewModelInstanceValueRuntime* value)
    {
        value->clearChanges();
    }

    // Number
    double rive_ViewModelInstanceNumberRuntime_Value(ViewModelInstanceNumberRuntime* value)
    {
        return value->value();
    }

    void rive_ViewModelInstanceNumberRuntime_SetValue(ViewModelInstanceNumberRuntime* value, double v)
    {
        value->value(v);
    }

    // String
    const char* rive_ViewModelInstanceStringRuntime_Value(ViewModelInstanceStringRuntime* value)
    {
        return value->value().c_str();
    }

    void rive_ViewModelInstanceStringRuntime_SetValue(ViewModelInstanceStringRuntime* value, const char* v)
    {
        value->value(std::string(v));
    }

    // Boolean
    bool rive_ViewModelInstanceBooleanRuntime_Value(ViewModelInstanceBooleanRuntime* value)
    {
        return value->value();
    }

    void rive_ViewModelInstanceBooleanRuntime_SetValue(ViewModelInstanceBooleanRuntime* value, bool v)
    {
        value->value(v);
    }

    // Color
    uint32_t rive_ViewModelInstanceColorRuntime_Value(ViewModelInstanceColorRuntime* value)
    {
        return value->value();
    }

    void rive_ViewModelInstanceColorRuntime_SetValue(ViewModelInstanceColorRuntime* value, uint32_t v)
    {
        value->value(v);
    }

    // Enum
    uint32_t rive_ViewModelInstanceEnumRuntime_ValueIndex(ViewModelInstanceEnumRuntime* value)
    {
        return value->valueIndex();
    }

    void rive_ViewModelInstanceEnumRuntime_SetValueIndex(ViewModelInstanceEnumRuntime* value, uint32_t v)
    {
        value->valueIndex(v);
    }

    // Trigger
    void rive_ViewModelInstanceTriggerRuntime_Trigger(ViewModelInstanceTriggerRuntime* value)
    {
        value->trigger();
    }

    // List
    size_t rive_ViewModelInstanceListRuntime_Size(ViewModelInstanceListRuntime* value)
    {
        return value->size();
    }

    ViewModelInstanceRuntime* rive_ViewModelInstanceListRuntime_At(ViewModelInstanceListRuntime* value, int index)
    {
        return value->instanceAt(index);
    }
}