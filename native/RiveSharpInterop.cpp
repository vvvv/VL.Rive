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


#define RIVE_DLL(RET) extern "C" __declspec(dllexport) RET __cdecl

// Native P/Invoke functions may only return "blittable" types. To protect from
// inadvertently returning an invalid type, we explicitly enumerate valid return
// types here. See:
// https://docs.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types
#define RIVE_DLL_VOID RIVE_DLL(void)
#define RIVE_DLL_INT8_BOOL RIVE_DLL(int8_t)
#define RIVE_DLL_INT32 RIVE_DLL(int32_t)
#define RIVE_DLL_FLOAT RIVE_DLL(float)
#define RIVE_DLL_INTPTR RIVE_DLL(intptr_t)

// Reverse P/Invoke Function pointers back into managed code are also __cdecl
// and may also only return blittable types.
#define RIVE_DELEGATE_VOID(NAME, ...) void(__cdecl * NAME)(__VA_ARGS__)
#define RIVE_DELEGATE_INTPTR(NAME, ...) intptr_t(__cdecl* NAME)(__VA_ARGS__)
#define RIVE_DELEGATE_INT32(NAME, ...) int32_t(__cdecl* NAME)(__VA_ARGS__)

////////////////////////////////////////////////////////////////////////////////////////////////////

RIVE_DLL_VOID Mat2D_Multiply(Mat2D a, Mat2D b, Mat2D* out) { *out = a * b; }
RIVE_DLL_VOID Mat2D_MultiplyVec2D(Mat2D a, Vec2D b, Vec2D* out)
{
    *out = a * b;
}
RIVE_DLL_INT8_BOOL Mat2D_Invert(Mat2D a, Mat2D* out) { return a.invert(out); }

////////////////////////////////////////////////////////////////////////////////////////////////////

class RendererSharp : public Renderer
{
public:
    struct Delegates
    {
        RIVE_DELEGATE_VOID(save, intptr_t ref);
        RIVE_DELEGATE_VOID(restore, intptr_t ref);
        RIVE_DELEGATE_VOID(transform,
                           intptr_t ref,
                           float,
                           float,
                           float,
                           float,
                           float,
                           float);
        RIVE_DELEGATE_VOID(drawPath,
                           intptr_t ref,
                           intptr_t path,
                           intptr_t paint);
        RIVE_DELEGATE_VOID(clipPath, intptr_t ref, intptr_t path);
        RIVE_DELEGATE_VOID(drawImage,
                           intptr_t ref,
                           intptr_t image,
                           int blendMode,
                           float opacity);
        RIVE_DELEGATE_VOID(drawImageMesh,
                           intptr_t ref,
                           intptr_t image,
                           const float* vertices,
                           const float* texcoords,
                           int vertexCount,
                           const uint16_t* indices,
                           int indexCount,
                           int blendMode,
                           float opacity);
    };

    static Delegates s_delegates;

    RendererSharp(intptr_t managedRef) : m_ref(managedRef) {}
    RendererSharp(const RendererSharp&) = delete;
    RendererSharp& operator=(const RendererSharp&) = delete;

    void save() override { s_delegates.save(m_ref); }
    void restore() override { s_delegates.restore(m_ref); }
    void transform(const Mat2D& m) override
    {
        s_delegates
            .transform(m_ref, m.xx(), m.xy(), m.yx(), m.yy(), m.tx(), m.ty());
    }
    void drawPath(RenderPath* path, RenderPaint* paint) override
    {
        s_delegates.drawPath(m_ref,
                             static_cast<RenderPathSharp*>(path)->m_ref,
                             static_cast<RenderPaintSharp*>(paint)->m_ref);
    }
    void clipPath(RenderPath* path) override
    {
        s_delegates.clipPath(m_ref, static_cast<RenderPathSharp*>(path)->m_ref);
    }
    void drawImage(const RenderImage* image,
                   BlendMode blendMode,
                   float opacity) override
    {
        s_delegates.drawImage(
            m_ref,
            static_cast<const RenderImageSharp*>(image)->m_ref,
            (int)blendMode,
            opacity);
    }
    void drawImageMesh(const RenderImage* image,
                       rcp<RenderBuffer> vertices_f32,
                       rcp<RenderBuffer> uvCoords_f32,
                       rcp<RenderBuffer> indices_u16,
                       uint32_t vertexCount,
                       uint32_t indexCount,
                       BlendMode blendMode,
                       float opacity) override
    {
        // We need our buffers and counts to agree.
        assert(vertices_f32->sizeInBytes() == vertexCount * sizeof(Vec2D));
        assert(uvCoords_f32->sizeInBytes() == vertexCount * sizeof(Vec2D));
        assert(indices_u16->sizeInBytes() == indexCount * sizeof(uint16_t));

        // The local matrix is ignored for SkCanvas::drawVertices, so we have to
        // manually scale the UVs to match Skia's convention.
        float w = (float)image->width();
        float h = (float)image->height();
        int n = vertexCount * 2;
        const float* uvs =
            static_cast<DataRenderBuffer*>(uvCoords_f32.get())->f32s();
        std::vector<float> denormUVs(n);
        for (int i = 0; i < n; i += 2)
        {
            denormUVs[i] = uvs[i] * w;
            denormUVs[i + 1] = uvs[i + 1] * h;
        }

        s_delegates.drawImageMesh(
            m_ref,
            static_cast<const RenderImageSharp*>(image)->m_ref,
            static_cast<DataRenderBuffer*>(vertices_f32.get())->f32s(),
            denormUVs.data(),
            vertexCount,
            static_cast<DataRenderBuffer*>(indices_u16.get())->u16s(),
            indexCount,
            (int)blendMode,
            opacity);
    }

private:
    intptr_t m_ref;
};

RendererSharp::Delegates RendererSharp::s_delegates{};

RIVE_DLL_VOID Renderer_RegisterDelegates(RendererSharp::Delegates delegates)
{
    RendererSharp::s_delegates = delegates;
}

struct ComputeAlignmentArgs
{
    int32_t fit;
    float alignX;
    float alignY;
    AABB frame;
    AABB content;
    Mat2D matrix;
};

RIVE_DLL_VOID Renderer_ComputeAlignment(ComputeAlignmentArgs* args)
{
    args->matrix = computeAlignment((Fit)args->fit,
                                    Alignment(args->alignX, args->alignY),
                                    args->frame,
                                    args->content);
}

////////////////////////////////////////////////////////////////////////////////////////////////////

class FactorySharp : public RenderContextD3DImpl
{
public:
    struct Delegates
    {
        RIVE_DELEGATE_VOID(release, intptr_t ref);
        RIVE_DELEGATE_INTPTR(makeRenderPath,
                             intptr_t ref,
                             intptr_t ptsArray, // Vec2D/SkPoint[nPts]
                             int nPts,
                             intptr_t verbsArray, // uint8_t/PathVerb[nPts]
                             int nVerbs,
                             int fillRule);
        RIVE_DELEGATE_INTPTR(makeEmptyRenderPath, intptr_t ref);
        RIVE_DELEGATE_INTPTR(makeRenderPaint, intptr_t ref);
        RIVE_DELEGATE_INTPTR(decodeImage,
                             intptr_t ref,
                             intptr_t bytesArray,
                             int nBytes);
    };

    static Delegates s_delegates;

    FactorySharp(intptr_t managedRef) : m_ref(managedRef) {}
    ~FactorySharp() { s_delegates.release(m_ref); }

    const intptr_t m_ref;
};

FactorySharp::Delegates FactorySharp::s_delegates{};

RIVE_DLL_VOID Factory_RegisterDelegates(FactorySharp::Delegates delegates)
{
    FactorySharp::s_delegates = delegates;
}

////////////////////////////////////////////////////////////////////////////////////////////////////

class NativeScene
{
public:
    NativeScene(std::unique_ptr<FactorySharp> factory) :
        m_Factory(std::move(factory))
    {}

    bool loadFile(const uint8_t* fileBytes, int length)
    {
        m_Scene.reset();
        m_Artboard.reset();
        m_File = File::import(Span<const uint8_t>(fileBytes, length),
                              m_Factory.get());
        return m_File != nullptr;
    }

    bool loadArtboard(const char* name)
    {
        m_Scene.reset();
        if (m_File)
        {
            m_Artboard = (name && name[0]) ? m_File->artboardNamed(name)
                                           : m_File->artboardDefault();
        }
        return m_Artboard != nullptr;
    }

    bool loadStateMachine(const char* name)
    {
        if (m_Artboard)
        {
            m_Scene = (name && name[0]) ? m_Artboard->stateMachineNamed(name)
                                        : m_Artboard->stateMachineAt(0);
        }
        return m_Scene != nullptr;
    }

    bool loadAnimation(const char* name)
    {
        if (m_Artboard)
        {
            m_Scene = (name && name[0]) ? m_Artboard->animationNamed(name)
                                        : m_Artboard->animationAt(0);
        }
        return m_Scene != nullptr;
    }

    bool setBool(const char* name, bool value)
    {
        if (SMIBool * input; m_Scene && (input = m_Scene->getBool(name)))
        {
            input->value(value);
            return true;
        }
        return false;
    }

    bool setNumber(const char* name, float value)
    {
        if (SMINumber * input; m_Scene && (input = m_Scene->getNumber(name)))
        {
            input->value(value);
            return true;
        }
        return false;
    }

    bool fireTrigger(const char* name)
    {
        if (SMITrigger * input; m_Scene && (input = m_Scene->getTrigger(name)))
        {
            input->fire();
            return true;
        }
        return false;
    }

    Scene* scene() { return m_Scene.get(); }

private:
    std::unique_ptr<FactorySharp> m_Factory;
    std::unique_ptr<File> m_File;
    std::unique_ptr<ArtboardInstance> m_Artboard;
    std::unique_ptr<Scene> m_Scene;
};

RIVE_DLL_INTPTR Scene_New(intptr_t managedFactory)
{
    return reinterpret_cast<intptr_t>(
        new NativeScene(std::make_unique<FactorySharp>(managedFactory)));
}

RIVE_DLL_VOID Scene_Delete(intptr_t ref)
{
    delete reinterpret_cast<NativeScene*>(ref);
}

RIVE_DLL_INT8_BOOL Scene_LoadFile(intptr_t ref,
                                  const uint8_t* fileBytes,
                                  int length)
{
    return reinterpret_cast<NativeScene*>(ref)->loadFile(fileBytes, length);
}

RIVE_DLL_INT8_BOOL Scene_LoadArtboard(intptr_t ref, const char* name)
{
    return reinterpret_cast<NativeScene*>(ref)->loadArtboard(name);
}

RIVE_DLL_INT8_BOOL Scene_LoadStateMachine(intptr_t ref, const char* name)
{
    return reinterpret_cast<NativeScene*>(ref)->loadStateMachine(name);
}

RIVE_DLL_INT8_BOOL Scene_LoadAnimation(intptr_t ref, const char* name)
{
    return reinterpret_cast<NativeScene*>(ref)->loadAnimation(name);
}

RIVE_DLL_INT8_BOOL Scene_SetBool(intptr_t ref, const char* name, int32_t value)
{
    return reinterpret_cast<NativeScene*>(ref)->setBool(name, value);
}

RIVE_DLL_INT8_BOOL Scene_SetNumber(intptr_t ref, const char* name, float value)
{
    return reinterpret_cast<NativeScene*>(ref)->setNumber(name, value);
}

RIVE_DLL_INT8_BOOL Scene_FireTrigger(intptr_t ref, const char* name)
{
    return reinterpret_cast<NativeScene*>(ref)->fireTrigger(name);
}

RIVE_DLL_FLOAT Scene_Width(intptr_t ref)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return scene->width();
    }
    return 0;
}

RIVE_DLL_FLOAT Scene_Height(intptr_t ref)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return scene->height();
    }
    return 0;
}

RIVE_DLL_INT32 Scene_Name(intptr_t ref, char* charArray)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        const std::string& name = scene->name();
        int32_t numChars = name.length();
        if (charArray)
        {
            memcpy(charArray, name.c_str(), numChars);
        }
        return numChars;
    }
    return 0;
}

RIVE_DLL_INT32 Scene_Loop(intptr_t ref)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return (int)reinterpret_cast<NativeScene*>(ref)->scene()->loop();
    }
    return 0;
}

RIVE_DLL_INT8_BOOL Scene_IsTranslucent(intptr_t ref)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return reinterpret_cast<NativeScene*>(ref)->scene()->isTranslucent();
    }
    return 0;
}

RIVE_DLL_FLOAT Scene_DurationSeconds(intptr_t ref)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return scene->durationSeconds();
    }
    return 0;
}

RIVE_DLL_INT8_BOOL Scene_AdvanceAndApply(intptr_t ref, float elapsedSeconds)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        return scene->advanceAndApply(elapsedSeconds);
    }
    return false;
}

RIVE_DLL_VOID Scene_Draw(intptr_t ref, intptr_t renderer)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        RendererSharp nativeRenderer(renderer);
        return scene->draw(&nativeRenderer);
    }
}

RIVE_DLL_VOID Scene_PointerDown(intptr_t ref, Vec2D pos)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        scene->pointerDown(pos);
    }
}

RIVE_DLL_VOID Scene_PointerMove(intptr_t ref, Vec2D pos)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        scene->pointerMove(pos);
    }
}

RIVE_DLL_VOID Scene_PointerUp(intptr_t ref, Vec2D pos)
{
    if (Scene* scene = reinterpret_cast<NativeScene*>(ref)->scene())
    {
        scene->pointerUp(pos);
    }
}
