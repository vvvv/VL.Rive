#include "RiveSharpInterop.hpp"
#include "rive/static_scene.hpp"
#include "rive/animation/linear_animation_instance.hpp"
#include <algorithm>
#include "rive/generated/viewmodel/viewmodel_property_viewmodel_base.hpp";
#include "utils/no_op_factory.hpp"
#include "rive/logging_scripting_context.hpp"
#include "rive/lua/scripting_vm.hpp"
#include "rive/lua/rive_lua_libs.hpp"
#include "rive/renderer/cmd/deferred_host.hpp"

using namespace rive;
using namespace rive::gpu;

// FrameDescriptor.cs mirrors RenderContext::FrameDescriptor by hand: keep both in sync.
// (RiveRenderContext checks the managed size against the same value.)
static_assert(sizeof(RenderContext::FrameDescriptor) == 72, "FrameDescriptor layout changed: update src/Interop/FrameDescriptor.cs");
static_assert(offsetof(RenderContext::FrameDescriptor, ditherMode) == 24, "FrameDescriptor layout changed");
static_assert(offsetof(RenderContext::FrameDescriptor, triangulationThresholds) == 32, "FrameDescriptor layout changed");
static_assert(offsetof(RenderContext::FrameDescriptor, wireframe) == 64, "FrameDescriptor layout changed");

static void (*g_scriptLogCallback)(int level, const char* message, size_t length) = nullptr;


#if defined(RIVE_CANVAS) && defined(RIVE_ORE)
// Records a file's drawing, including what its scripts do on the GPU, into a deferred session and replays it
// against the render context once per frame. The plain render context cannot host GPU scripts: a script's
// render pass cannot nest inside the open screen frame, so upstream's own hosts all go through a session.
struct DeferredHostD3D11
{
    DeferredHostD3D11(RenderContext* renderContext, ore::Context* ore)
        : m_renderContext(renderContext)
        , m_session(ore::ReplayCaps::from(*ore))
    {
        m_session.bindRenderContext(renderContext);
        m_host.bindSession(&m_session);
    }

    Factory* sessionFactory() { return static_cast<Factory*>(&m_session); }

    Renderer* beginFrame(uint32_t width, uint32_t height)
    {
        m_width = width;
        m_height = height;
        // Scripts may draw into the render target under Rive's content: tell them its size and format
        m_host.beginRecord(
            /*clear*/ false,
            /*color*/ 0,
            ore::Context::TargetDesc::color8(width, height, /*bgra*/ false));
        return m_host.screenRenderer();
    }

    void endFrame(RenderTarget* renderTarget, uint64_t frame, uint64_t safeFrame)
    {
        m_renderTarget = renderTarget;
        m_frame = frame;
        m_safeFrame = safeFrame;

        // A sink is a per frame object: it remembers the screen renderer it opened, so one that outlived the
        // frame would hand the next replay a dangling renderer.
        Sink sink(*this);
        m_host.replayInline(sink, [this]() {
            RenderContext::FlushResources resources;
            resources.renderTarget = m_renderTarget;
            resources.currentFrameNumber = m_frame;
            resources.safeFrameNumber = m_safeFrame;
            m_renderContext->flush(resources);
        });
        m_renderTarget = nullptr;
    }

private:
    class Sink final : public cmd::HostFrameSink
    {
    public:
        explicit Sink(DeferredHostD3D11& host)
            : cmd::HostFrameSink(/*clear*/ false, /*color*/ 0)
            , m_owner(host)
        {
            // D3D11 flushes without an external command buffer
            m_useExternalCommandBuffer = false;
        }

        RenderContext* renderContext() override { return m_owner.m_renderContext; }

        Renderer* beginScreen(uint64_t, bool clear, uint32_t color) override
        {
            RenderContext::FrameDescriptor descriptor;
            descriptor.renderTargetWidth = m_owner.m_width;
            descriptor.renderTargetHeight = m_owner.m_height;
            descriptor.loadAction = clear ? LoadAction::clear : LoadAction::preserveRenderTarget;
            descriptor.clearColor = color;
            m_owner.m_renderContext->beginFrame(descriptor);
            m_screen = std::make_unique<RiveRenderer>(m_owner.m_renderContext);
            return m_screen.get();
        }

        RenderTarget* targetRenderTarget() override { return m_owner.m_renderTarget; }

        RenderContext::FlushResources canvasFlushResources() override
        {
            RenderContext::FlushResources resources;
            resources.currentFrameNumber = m_owner.m_frame;
            resources.safeFrameNumber = m_owner.m_safeFrame;
            return resources;
        }

    private:
        DeferredHostD3D11& m_owner;
        std::unique_ptr<RiveRenderer> m_screen;
    };

    RenderContext* m_renderContext;
    cmd::DeferredSession m_session;
    cmd::DeferredInlineHost m_host;
    RenderTarget* m_renderTarget = nullptr;
    uint32_t m_width = 0;
    uint32_t m_height = 0;
    uint64_t m_frame = 0;
    uint64_t m_safeFrame = 0;
};
#endif

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

    Factory* rive_Factory_Create()
    {
        return new NoOpFactory();
    }

    void rive_Factory_Destroy(Factory* factory)
    {
        delete factory;
    }

    RenderImage* rive_Factory_DecodeImage(Factory* factory, uint8_t* data, int dataLength)
    {
        auto image = factory->decodeImage(rive::Span<const uint8_t>(data, dataLength));
        return image ? image.release() : nullptr;
    }

    Font* rive_Factory_DecodeFont(Factory* factory, uint8_t* data, int dataLength)
    {
        auto font = factory->decodeFont(rive::Span<const uint8_t>(data, dataLength));
        return font ? font.release() : nullptr;
    }

    AudioSource* rive_Factory_DecodeAudio(Factory* factory, uint8_t* data, int dataLength)
    {
        auto audio = factory->decodeAudio(rive::Span<const uint8_t>(data, dataLength));
        return audio ? audio.release() : nullptr;
    }

    // Resource management - properly release reference-counted resources
    void rive_RenderImage_Release(RenderImage* image)
    {
        if (image != nullptr)
        {
            rcp<RenderImage> adopted(image); // Adopt the reference we handed out; letting the rcp go out of scope drops it
        }
    }

    void rive_Font_Release(Font* font)
    {
        if (font != nullptr)
        {
            rcp<Font> adopted(font); // Adopt the reference we handed out; letting the rcp go out of scope drops it
        }
    }

    void rive_AudioSource_Release(AudioSource* audioSource)
    {
        if (audioSource != nullptr)
        {
            rcp<AudioSource> adopted(audioSource); // Adopt the reference we handed out; letting the rcp go out of scope drops it
        }
    }

    void rive_BindableArtboard_Release(BindableArtboard* artboard)
    {
        if (artboard != nullptr)
        {
            rcp<BindableArtboard> adopted(artboard); // Adopt the reference we handed out; letting the rcp go out of scope drops it
        }
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

    void rive_RenderContext_Flush(RenderContext* self, RenderTarget* renderTarget, uint64_t currentFrameNumber, uint64_t safeFrameNumber)
    {
        self->flush({
			.renderTarget = renderTarget,
			.currentFrameNumber = currentFrameNumber,
			.safeFrameNumber = safeFrameNumber,
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

    // Deferred recording host (GPU scripting)
    DeferredHostD3D11* rive_Deferred_Create(RenderContext* renderContext)
    {
#if defined(RIVE_CANVAS) && defined(RIVE_ORE)
        if (auto* ore = renderContext->ore())
            return new DeferredHostD3D11(renderContext, ore);
#endif
        return nullptr;
    }

    void rive_Deferred_Destroy(DeferredHostD3D11* host)
    {
#if defined(RIVE_CANVAS) && defined(RIVE_ORE)
        delete host;
#endif
    }

    Factory* rive_Deferred_Factory(DeferredHostD3D11* host)
    {
#if defined(RIVE_CANVAS) && defined(RIVE_ORE)
        return host->sessionFactory();
#else
        return nullptr;
#endif
    }

    Renderer* rive_Deferred_BeginFrame(DeferredHostD3D11* host, uint32_t width, uint32_t height)
    {
#if defined(RIVE_CANVAS) && defined(RIVE_ORE)
        return host->beginFrame(width, height);
#else
        return nullptr;
#endif
    }

    void rive_Deferred_EndFrame(DeferredHostD3D11* host, RenderTarget* renderTarget, uint64_t currentFrameNumber, uint64_t safeFrameNumber)
    {
#if defined(RIVE_CANVAS) && defined(RIVE_ORE)
        host->endFrame(renderTarget, currentFrameNumber, safeFrameNumber);
#endif
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

    // Scripting
    void rive_Scripting_SetLogCallback(void (*callback)(int level, const char* message, size_t length))
    {
        g_scriptLogCallback = callback;
    }

    // File
    File* rive_File_Import(uint8_t* data, int dataLength, Factory* factory, RiveImportResult* result)
    {
        ImportResult importResult = ImportResult::success;

        // Without a context of our own, script console output goes to stdout and errors to nowhere:
        // give the file a scripting VM that reports to the host's log callback.
        rcp<ScriptingVM> scriptingVM;
        if (g_scriptLogCallback != nullptr)
        {
            auto makeContext = makeLoggingScriptingContextFactory(
                [](ScriptingLogLevel level, const char* message, size_t length)
                {
                    if (g_scriptLogCallback != nullptr)
                        g_scriptLogCallback(static_cast<int>(level), message, length);
                });
            if (auto context = makeContext(factory))
                scriptingVM = make_rcp<ScriptingVM>(std::move(context));
        }

        auto file = File::import(
            rive::Span<const uint8_t>(data, dataLength),
            factory,
            &importResult,
            static_cast<FileAssetLoader*>(nullptr),
            scriptingVM.get());
        if (result != nullptr)
        {
            *result = static_cast<RiveImportResult>(importResult);
        }
        return file ? file.release() : nullptr;
    }

    void rive_File_Destroy(File* file)
    {
        delete file;
    }

    int rive_File_ArtboardCount(File* file)
    {
		return file->artboardCount();
    }

    void rive_File_Artboards(File* file, Artboard** artboards_out)
    {
        auto artboards = file->artboards();
        for (int i = 0; i < artboards.size(); ++i)
        {
            artboards_out[i] = artboards[i];
		}
    }

    ArtboardInstance* rive_File_ArtboardByName(File* file, const char* name)
    {
        return file->artboardNamed(name).release();
    }

    ArtboardInstance* rive_File_GetArtboardDefault(File* file)
    {
        return file->artboardDefault().release();
    }

    BindableArtboard* rive_File_BindableArtboardNamed(File* file, const char* name)
    {
        auto bindableArtboard = file->bindableArtboardNamed(name);
        return bindableArtboard ? bindableArtboard.release() : nullptr;
    }

    BindableArtboard* rive_File_BindableArtboardDefault(File* file)
    {
        auto bindableArtboard = file->bindableArtboardDefault();
        return bindableArtboard ? bindableArtboard.release() : nullptr;
    }

    ViewModelRuntime* rive_File_DefaultArtboardViewModel(File* file, Artboard* artboard)
    {
        return file->defaultArtboardViewModel(artboard);
    }

    int rive_File_ViewModelCount(File* file)
    {
        return file->viewModelCount();
    }

    void rive_File_GetViewModel(File* file, int index, char** name_out, int* propertiesCount_out)
    {
        auto viewModel = file->viewModelByIndex(index);
        viewModel->properties();
        *name_out = _strdup(viewModel->name().c_str());
        *propertiesCount_out = viewModel->propertyCount();
    }

    void rive_File_GetViewModelProperties(File* file, int index, RivePropertyData* properties_out)
    {
        auto viewModel = file->viewModel(index);
        auto viewModelRuntime = file->viewModelByIndex(index);
        // Fill properties
        auto rprops = viewModelRuntime->properties();
        auto props = viewModel->properties();
        for (int i = 0; i < props.size(); ++i)
        {
            properties_out[i].type = static_cast<int>(rprops[i].type);
            properties_out[i].name = _strdup(rprops[i].name.c_str());
            if (rprops[i].type == DataType::viewModel)
            {
                auto v = props[i]->as<ViewModelPropertyViewModelBase>();
                properties_out[i].viewModelReferenceId = v->viewModelReferenceId();
            }
        }
    }

    ViewModelRuntime* rive_File_ViewModelByIndex(File* file, int index)
    {
        return file->viewModelByIndex(index);
    }

    ViewModelRuntime* rive_File_ViewModelByName(File* file, const char* name)
    {
        return file->viewModelByName(std::string(name));
    }

    // Artboard
    const char* rive_Artboard_Name(Artboard* artboard)
    {
		return artboard->name().c_str();
    }

    int rive_Artboard_StateMachineCount(Artboard* artboard)
    {
        return artboard->stateMachineCount();
    }

    void rive_Artboard_StateMachines(Artboard* artboard, StateMachine** machines_out)
    {
        for (int i = 0; i < artboard->stateMachineCount(); ++i)
        {
            machines_out[i] = artboard->stateMachine(i);
		}
    }

    int rive_Artboard_AnimationCount(Artboard* artboard)
    {
		return artboard->animationCount();
    }

    void rive_Artboard_Animations(Artboard* artboard, Animation** animations_out)
    {
        for (int i = 0; i < artboard->animationCount(); ++i)
        {
            animations_out[i] = artboard->animation(i);
        }
    }

    // Resizes the artboard, which lays out its content again (Fit.Layout). Rive expects this before advancing.
    void rive_Artboard_SetSize(Artboard* artboard, float width, float height)
    {
        artboard->width(width);
        artboard->height(height);
    }

    // Back to the size the artboard has in the file
    void rive_Artboard_ResetSize(Artboard* artboard)
    {
        artboard->resetSize();
    }

    RiveAABB rive_Artboard_Bounds(Artboard* artboard)
    {
        AABB bounds = artboard->bounds();
        RiveAABB result;
        result.minX = bounds.minX;
        result.minY = bounds.minY;
        result.maxX = bounds.maxX;
        result.maxY = bounds.maxY;
        return result;
    }

    uint32_t rive_Artboard_ViewModelId(Artboard* artboard)
    {
        return artboard->viewModelId();
    }

	// StateMachine
    const char* rive_StateMachine_Name(StateMachine* stateMachine)
    {
        return stateMachine->name().c_str();
    }

    // Animation
    const char* rive_Animation_Name(Animation* animation)
    {
        return animation->name().c_str();
    }

	// ArtboardInstance
    Scene* rive_ArtboardInstance_SceneByName(ArtboardInstance* artboard, const char* name)
    {
        auto sm = artboard->stateMachineNamed(name);
        if (sm != nullptr)
            return sm.release();

        auto animation = artboard->animationNamed(name);
        if (animation != nullptr)
            return animation.release();

        return nullptr;
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
        artboard->bindViewModelInstance(ref_rcp(viewModelInstance));
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

    // Sets the point in time of an animation (0 = start, 1 = end of its work area) and applies it.
    // Returns false if the scene is not an animation with a duration (a state machine has no time to seek).
    bool rive_Scene_SeekNormalized(Scene* scene, float position)
    {
        // RTTI is disabled, so no dynamic_cast. Of the three kinds of scene only an animation has a duration above 0:
        // a state machine reports -1 and a static scene 0.
        if (scene->durationSeconds() <= 0.0f)
            return false;
        auto animation = static_cast<LinearAnimationInstance*>(scene);

        position = std::min(std::max(position, 0.0f), 1.0f);
        const float duration = animation->durationSeconds();
        // startTime is the end of the work area for an animation that plays backwards
        const float start = animation->speed() >= 0 ? animation->startTime() : animation->startTime() - duration;
        animation->time(start + position * duration);
        animation->advanceAndApply(0.0f);
        return true;
    }

    void rive_Scene_BindViewModelInstance(Scene* scene, ViewModelInstance* viewModelInstance)
    {
        scene->bindViewModelInstance(ref_rcp(viewModelInstance));
    }

    RiveHitResult rive_Scene_PointerDown(Scene* scene, float x, float y, int pointerId)
    {
		return static_cast<RiveHitResult>(scene->pointerDown(Vec2D(x, y), pointerId));
    }

    RiveHitResult rive_Scene_PointerMove(Scene* scene, float x, float y, float timeStamp, int pointerId)
    {
        return static_cast<RiveHitResult>(scene->pointerMove(Vec2D(x, y), timeStamp, pointerId));
    }

    RiveHitResult rive_Scene_PointerUp(Scene* scene, float x, float y, int pointerId)
    {
        return static_cast<RiveHitResult>(scene->pointerUp(Vec2D(x, y), pointerId));
    }

    RiveHitResult rive_Scene_PointerExit(Scene* scene, float x, float y, int pointerId)
    {
        return static_cast<RiveHitResult>(scene->pointerExit(Vec2D(x, y), pointerId));
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
        return runtime->createInstance().release();
    }

    ViewModelInstanceRuntime* rive_ViewModelRuntime_CreateDefaultInstance(ViewModelRuntime* runtime)
    {
        return runtime->createDefaultInstance().release();
    }

    int rive_ViewModelRuntime_InstanceCount(ViewModelRuntime* runtime)
    {
        return (int)runtime->instanceCount();
    }

    const char* rive_ViewModelRuntime_InstanceNameAt(ViewModelRuntime* runtime, int index)
    {
        auto names = runtime->instanceNames();
        if (index < 0 || (size_t)index >= names.size())
            return nullptr;
        return _strdup(names[(size_t)index].c_str());
    }

    ViewModelInstanceRuntime* rive_ViewModelRuntime_CreateInstanceFromIndex(ViewModelRuntime* runtime, int index)
    {
        if (index < 0 || (size_t)index >= runtime->instanceCount())
            return nullptr;
        return runtime->createInstanceFromIndex((size_t)index).release();
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
        return runtime->propertyViewModel(std::string(path)).release();
    }

    ViewModelInstanceAssetImageRuntime* rive_ViewModelInstanceRuntime_PropertyImage(ViewModelInstanceRuntime* runtime, const char* path)
    {
        return runtime->propertyImage(std::string(path));
    }

    ViewModelInstanceArtboardRuntime* rive_ViewModelInstanceRuntime_PropertyArtboard(ViewModelInstanceRuntime* runtime, const char* path)
    {
        return runtime->propertyArtboard(std::string(path));
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
        // Borrowed: valid while the runtime is alive. Callers that keep it (binding) take their own reference.
        return runtime->instance().get();
    }

    void rive_ViewModelInstanceRuntime_Release(ViewModelInstanceRuntime* runtime)
    {
        // Adopt the reference we handed out; letting the rcp go out of scope drops it
        // (a named variable: "rcp<T>(x);" alone would declare an empty variable called x)
        rcp<ViewModelInstanceRuntime> adopted(runtime);
    }

    bool rive_ViewModelInstanceRuntime_ReplaceViewModel(ViewModelInstanceRuntime* runtime, const char* path, ViewModelInstanceRuntime* value)
    {
        return runtime->replaceViewModel(std::string(path), value);
    }

    uint32_t rive_ViewModelInstance_ViewModelId(ViewModelInstance* instance)
    {
        return instance->viewModelId();
    }

    bool rive_ViewModelInstanceValueRuntime_HasChanged(ViewModelInstanceValueRuntime* value)
    {
        return value->hasChanged();
    }

    void rive_ViewModelInstanceValueRuntime_ClearChanges(ViewModelInstanceValueRuntime* value)
    {
        value->clearChanges();
    }

    RiveDataType rive_ViewModelInstanceValueRuntime_DataType(ViewModelInstanceValueRuntime* value)
    {
        return static_cast<RiveDataType>(value->dataType());
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
    const char* rive_ViewModelInstanceEnumRuntime_Value(ViewModelInstanceEnumRuntime* value)
    {
        // value() returns by value: hand out a copy that the caller frees
        return _strdup(value->value().c_str());
    }

    void rive_ViewModelInstanceEnumRuntime_SetValue(ViewModelInstanceEnumRuntime* value, const char* v)
    {
        value->value(std::string(v));
    }

    uint32_t rive_ViewModelInstanceEnumRuntime_ValueIndex(ViewModelInstanceEnumRuntime* value)
    {
        return value->valueIndex();
    }

    void rive_ViewModelInstanceEnumRuntime_SetValueIndex(ViewModelInstanceEnumRuntime* value, uint32_t v)
    {
        value->valueIndex(v);
    }

    size_t rive_ViewModelInstanceEnumRuntime_ValuesCount(ViewModelInstanceEnumRuntime* value)
    {
        return value->values().size();
    }

    const char* rive_ViewModelInstanceEnumRuntime_ValueAt(ViewModelInstanceEnumRuntime* value, size_t index)
    {
        auto values = value->values();
        if (index < values.size())
        {
            return _strdup(values[index].c_str());
        }
        return nullptr;
    }

    const char* rive_ViewModelInstanceEnumRuntime_EnumType(ViewModelInstanceEnumRuntime* value)
    {
        // enumType() returns by value: hand out a copy that the caller frees
        return _strdup(value->enumType().c_str());
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
        return value->instanceAt(index).release();
    }

    void rive_ViewModelInstanceListRuntime_AddInstance(ViewModelInstanceListRuntime* value, ViewModelInstanceRuntime* instance)
    {
        value->addInstance(instance);
    }

    bool rive_ViewModelInstanceListRuntime_AddInstanceAt(ViewModelInstanceListRuntime* value, ViewModelInstanceRuntime* instance, int index)
    {
        return value->addInstanceAt(instance, index);
    }

    void rive_ViewModelInstanceListRuntime_RemoveInstance(ViewModelInstanceListRuntime* value, ViewModelInstanceRuntime* instance)
    {
        value->removeInstance(instance);
    }

    void rive_ViewModelInstanceListRuntime_RemoveInstanceAt(ViewModelInstanceListRuntime* value, int index)
    {
        value->removeInstanceAt(index);
    }

    void rive_ViewModelInstanceListRuntime_Swap(ViewModelInstanceListRuntime* value, uint32_t index1, uint32_t index2)
    {
        value->swap(index1, index2);
    }

    // AssetImage
    void rive_ViewModelInstanceAssetImageRuntime_SetValue(ViewModelInstanceAssetImageRuntime* value, RenderImage* renderImage)
    {
        value->value(renderImage);
    }

    // Artboard
    void rive_ViewModelInstanceArtboardRuntime_SetValue(ViewModelInstanceArtboardRuntime* value, BindableArtboard* bindableArtboard)
    {
        value->value(ref_rcp(bindableArtboard)); // takes its own reference: the caller still releases theirs
    }
}