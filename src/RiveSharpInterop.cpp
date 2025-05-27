#include "RiveSharpInterop.hpp"

extern "C"
{
    __declspec(dllexport) RenderContext* CreateRenderContextD3D11(ID3D11Device* device, ID3D11DeviceContext* deviceContext)
    {
		D3DContextOptions contextOptions;
        return RenderContextD3DImpl::MakeContext(device, deviceContext, contextOptions).release();
	}
}
