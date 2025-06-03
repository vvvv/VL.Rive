#include "RiveSharpInterop.hpp"
#include "rive/static_scene.hpp"

using namespace rive;
using namespace rive::gpu;

extern "C"
{
    // Math
    RiveMat2D rive_ComputeAlignment(
        RiveFit fit,
        RiveAlignment alignment,
        RiveAABB frame,
        RiveAABB content,
        float scaleFactor)
    {
        // Convert C types to C++ types
        rive::Fit cppFit = static_cast<rive::Fit>(fit);
        rive::Alignment cppAlignment(alignment.x, alignment.y);
        rive::AABB cppFrame(frame.minX, frame.minY, frame.maxX, frame.maxY);
        rive::AABB cppContent(content.minX, content.minY, content.maxX, content.maxY);

        rive::Mat2D mat = rive::computeAlignment(cppFit, cppAlignment, cppFrame, cppContent, scaleFactor);

        // Copy result to result
        RiveMat2D result;
        memcpy(result.values, mat.values(), sizeof(float) * 6);
        return result;
    }

    RiveMat2D rive_Mat2D_InvertOrIdentity(const RiveMat2D* inMat)
    {
        rive::Mat2D cppMat(
            inMat->values[0], inMat->values[1],
            inMat->values[2], inMat->values[3],
            inMat->values[4], inMat->values[5]);

        RiveMat2D result;
        memcpy(result.values, cppMat.invertOrIdentity().values(), sizeof(float) * 6);
        return result;
    }

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

    void rive_RenderContext_BeginFrame(RenderContext* self, const RenderContext::FrameDescriptor* frameDesc)
    {
        self->beginFrame(*frameDesc);
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

    void rive_Renderer_Save(Renderer* renderer)
    {
        renderer->save();
    }

    void rive_Renderer_Restore(Renderer* renderer)
    {
        renderer->restore();
    }

    void rive_Renderer_Transform(Renderer* renderer, const RiveMat2D* mat)
    {
        rive::Mat2D cppMat(
            mat->values[0], mat->values[1],
            mat->values[2], mat->values[3],
            mat->values[4], mat->values[5]);
        renderer->transform(cppMat);
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
    void rive_Scene_Draw(Scene* scene, Renderer* renderer)
    {
        scene->draw(renderer);
    }

    void rive_Scene_Destroy(Scene* self)
    {
		delete self;
    }

    float rive_Scene_Width(Scene* scene)
    {
        return scene->width();
    }

    float rive_Scene_Height(Scene* scene)
    {
        return scene->height();
    }

    RiveAABB rive_Scene_Bounds(Scene* scene)
    {
        AABB bounds = scene->bounds();
        RiveAABB result;
        result.minX = bounds.minX;
        result.minY = bounds.minY;
        result.maxX = bounds.maxX;
        result.maxY = bounds.maxY;
        return result;
    }

    const char* rive_Scene_Name(Scene* scene)
    {
        return _strdup(scene->name().c_str());
    }

    int rive_Scene_Loop(Scene* scene)
    {
        return static_cast<int>(scene->loop());
    }

    bool rive_Scene_IsTranslucent(Scene* scene)
    {
        return scene->isTranslucent();
    }

    float rive_Scene_DurationSeconds(Scene* scene)
    {
        return scene->durationSeconds();
    }

    bool rive_Scene_AdvanceAndApply(Scene* scene, float elapsedSeconds)
    {
        return scene->advanceAndApply(elapsedSeconds);
    }

    void rive_Scene_BindViewModelInstance(Scene* scene, ViewModelInstance* viewModelInstance)
    {
		// TODO: Needed?
        viewModelInstance->ref();
        scene->bindViewModelInstance(rcp<ViewModelInstance>(viewModelInstance));
    }

    RiveHitResult rive_Scene_PointerDown(Scene* scene, float x, float y)
    {
		return static_cast<RiveHitResult>(scene->pointerDown(Vec2D(x, y)));
    }

    RiveHitResult rive_Scene_PointerMove(Scene* scene, float x, float y)
    {
        return static_cast<RiveHitResult>(scene->pointerMove(Vec2D(x, y)));
    }

    RiveHitResult rive_Scene_PointerUp(Scene* scene, float x, float y)
    {
        return static_cast<RiveHitResult>(scene->pointerUp(Vec2D(x, y)));
    }

    RiveHitResult rive_Scene_PointerExit(Scene* scene, float x, float y)
    {
        return static_cast<RiveHitResult>(scene->pointerExit(Vec2D(x, y)));
    }

    size_t rive_Scene_InputCount(Scene* scene)
    {
        return scene->inputCount();
    }

    SMIInput* rive_Scene_Input(Scene* scene, size_t index)
    {
        return scene->input(index);
    }

    SMIBool* rive_Scene_GetBool(Scene* scene, const char* name)
    {
        return scene->getBool(std::string(name));
    }

    SMINumber* rive_Scene_GetNumber(Scene* scene, const char* name)
    {
        return scene->getNumber(std::string(name));
    }

    SMITrigger* rive_Scene_GetTrigger(Scene* scene, const char* name)
    {
        return scene->getTrigger(std::string(name));
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

    int rive_ViewModelInstanceRuntime_PropertyCount(ViewModelInstanceRuntime* runtime)
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
            properties_out[i].name = _strdup(props[i].name.c_str());
        }
    }

    ViewModelInstance* rive_ViewModelInstanceRuntime_Instance(ViewModelInstanceRuntime* runtime)
    {
        return runtime->instance().release();
    }

    void rive_ViewModelInstanceRuntime_Destroy(ViewModelInstanceRuntime* runtime)
    {
		delete runtime;
    }

    bool rive_ViewModelInstanceValueRuntime_HasChanged(ViewModelInstanceValueRuntime* value)
    {
        return value->hasChanged();
    }

    void rive_ViewModelInstanceValueRuntime_ClearChanges(ViewModelInstanceValueRuntime* value)
    {
        value->clearChanges();
    }

    void rive_ViewModelInstanceValueRuntime_Destroy(ViewModelInstanceValueRuntime* value)
    {
		delete value;
    }

    // Number
    float rive_ViewModelInstanceNumberRuntime_Value(ViewModelInstanceNumberRuntime* value)
    {
        return value->value();
    }

    void rive_ViewModelInstanceNumberRuntime_SetValue(ViewModelInstanceNumberRuntime* value, float v)
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