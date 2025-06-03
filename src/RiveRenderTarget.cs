using static RiveSharpInterop.Methods;

namespace VL.Rive
{
    internal abstract class RiveRenderTarget : RiveObject
    {
        public RiveRenderTarget(nint handle) : base(handle)
        {
        }

        protected override bool ReleaseHandle()
        {
            rive_RenderTarget_Destroy(handle);
            return true;
        }
    }

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
}