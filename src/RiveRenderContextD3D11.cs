using static VL.Rive.Interop.Methods;

namespace VL.Rive;

internal class RiveRenderContextD3D11 : RiveRenderContext
{
    public static RiveRenderContextD3D11 Create(nint device, nint deviceContext)
    {
        var handle = rive_RenderContext_Create_D3D11(device, deviceContext);
        if (handle == nint.Zero)
        {
            throw new InvalidOperationException("Failed to create RiveRenderContext for D3D11.");
        }
        return new RiveRenderContextD3D11(handle, device);
    }

    public RiveRenderContextD3D11(nint handle, nint devicePointer) : base(handle)
    {
        DevicePointer = devicePointer;
    }

    public nint DevicePointer { get; }

    public RiveRenderTargetD3D11 MakeRenderTarget(int width, int height)
    {
        var handle = rive_RenderContext_MakeRenderTarget_D3D11(this.handle, width, height);
        if (handle == nint.Zero)
        {
            throw new InvalidOperationException("Failed to create RiveRenderTarget for D3D11.");
        }
        return new RiveRenderTargetD3D11(handle);
    }
}