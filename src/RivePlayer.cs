using Microsoft.Extensions.DependencyInjection;
using SharpDX.Direct3D11;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Animation;
using VL.Lib.IO;
using Path = VL.Lib.IO.Path;

namespace VL.Rive;

[ProcessNode(HasStateOutput = true)]
public sealed class RivePlayer : RendererBase
{
    Path? file;
    RiveRenderContextD3D11? riveRenderContext;
    RiveRenderTargetD3D11? riveRenderTarget;

    RiveRenderer? riveRenderer;
    RiveFile? riveFile;
    RiveArtboard? riveArtboard;
    RiveScene? riveScene;
    private nint riveViewModel;
    private ViewModelInstance? riveViewModelInstance;
    bool needsReload;
    private IFrameClock frameClock;
    Int2 lastSize;

    public RivePlayer(NodeContext nodeContext)
    {
        frameClock = nodeContext.AppHost.Services.GetRequiredService<IFrameClock>();
    }

    public void Update(Path? file)
    {
        if (file != this.file)
        {
            this.file = file;
            needsReload = true;
        }
    }

    protected override unsafe void DrawCore(RenderDrawContext context)
    {
        var graphicsDevice = context.GraphicsDevice;
        if (riveRenderContext is null)
            riveRenderContext = CreateRiveRenderContext(graphicsDevice);
        if (riveRenderContext is null)
            return;

        var renderTarget = context.CommandList.RenderTarget;

        var size = new Int2(renderTarget.Width, renderTarget.Height);
        if (riveRenderer is null || lastSize != size)
        {
            lastSize = size;

            riveRenderer?.Dispose();
            riveRenderTarget?.Dispose();

            riveRenderer = riveRenderContext.CreateRenderer();
            riveRenderTarget = riveRenderContext.MakeRenderTarget(size.X, size.Y);
        }

        if (needsReload)
        {
            needsReload = false;

            riveScene?.Dispose();
            riveArtboard?.Dispose();
            riveFile?.Dispose();

            if (file != null)
                riveFile = riveRenderContext.LoadFile(file);
            else
                riveFile = null;

            if (riveFile != null)
            {
                riveArtboard = riveFile.GetArtboardDefault();
                riveScene = riveArtboard.DefaultScene();
                //riveViewModel = rive_File_DefaultArtboardViewModel(riveFile, riveArtboard);
                //if (riveViewModel != default)
                //{
                //    riveViewModelInstance = new ViewModelInstance(rive_ViewModelRuntime_CreateInstance(riveViewModel));
                //    var proprs = riveViewModelInstance.Properties;
                //}
            }
        }

        if (riveScene is null || riveRenderTarget is null)
            return;

        riveScene.AdvanceAndApply((float)frameClock.TimeDifference);

        var frameDescriptor = new RiveSharpInterop.FrameDescriptor
        {
            RenderTargetWidth = (uint)renderTarget.Width,
            RenderTargetHeight = (uint)renderTarget.Height,
            MsaaSampleCount = renderTarget.MultisampleCount != MultisampleCount.None ? (int)renderTarget.MultisampleCount : 0,
        };
        riveRenderContext.BeginFrame(in frameDescriptor);
        riveScene.Draw(riveRenderer, size.X, size.Y);

        var nativeRenderTarget = SharpDXInterop.GetNativeResource(renderTarget) as Texture2D;
        if (nativeRenderTarget is null)
            return;
        riveRenderTarget.SetTargetTexture(nativeRenderTarget.NativePointer);
        riveRenderContext.Flush(riveRenderTarget);

        // Release render target texture
        riveRenderTarget.SetTargetTexture(default);
    }

    protected override void Destroy()
    {
        riveScene?.Dispose();
        riveArtboard?.Dispose();
        riveFile?.Dispose();
        riveRenderer?.Dispose();
        riveRenderTarget?.Dispose();
        riveRenderContext?.Dispose();

        base.Destroy();
    }

    private RiveRenderContextD3D11? CreateRiveRenderContext(GraphicsDevice device)
    {
        var nativeDevice = SharpDXInterop.GetNativeDevice(device) as Device;
        var nativeContext = SharpDXInterop.GetNativeDeviceContext(device) as DeviceContext;
        if (nativeDevice is null || nativeContext is null)
            return default;

        return RiveRenderContextD3D11.Create(nativeDevice.NativePointer, nativeContext.NativePointer);
    }
}
