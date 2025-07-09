using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal class RiveRenderTargetD3D11 : RiveRenderTarget
{
    public RiveRenderTargetD3D11(nint handle) : base(handle)
    {
    }
    
    public void SetTargetTexture(nint textureHandle)
    {
        rive_RenderTarget_D3D11_SetTargetTexture(handle, textureHandle);
    }
}