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

extern "C"
{
    __declspec(dllexport) intptr_t CreateRenderContextD3D11(ID3D11Device* device, ID3D11DeviceContext* deviceContext, const D3DContextOptions& contextOptions)
    {
        return reinterpret_cast<intptr_t>(RenderContextD3DImpl::MakeContext(device, deviceContext, contextOptions).release());
	}
}
