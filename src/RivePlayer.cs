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
using static RiveSharpInterop.Methods;
using Path = VL.Lib.IO.Path;

namespace VL.Rive
{
    [ProcessNode(HasStateOutput = true)]
    public sealed class RivePlayer : RendererBase
    {
        Path? file;
        nint riveRenderContext;
        nint riveRenderer;
        nint riveRenderTarget;


        nint riveFile;
        nint riveArtboard;
        nint riveScene;
        bool needsReload;
        private IFrameClock? frameClock;
        Int2 lastSize;

        public RivePlayer(NodeContext nodeContext)
        {
            frameClock = nodeContext.AppHost.Services.GetService<IFrameClock>();
        }

        public void Update(Path? file)
        {
            if (file != this.file)
            {
                this.file = file;
                needsReload = true;
            }
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var graphicsDevice = context.GraphicsDevice;
            if (riveRenderContext == 0)
                riveRenderContext = CreateRiveRenderContext(graphicsDevice);
            if (riveRenderContext == 0)
                return;

            var renderTarget = context.CommandList.RenderTarget;

            var size = new Int2(renderTarget.Width, renderTarget.Height);
            if (riveRenderer == 0 || lastSize != size)
            {
                lastSize = size;
                riveRenderer = rive_Renderer_Create(riveRenderContext);
                riveRenderTarget = MakeRenderTarget(size.X, size.Y);
            }

            if (needsReload)
            {
                if (riveArtboard != 0)
                {
                    rive_ArtboardInstance_Destroy(riveArtboard);
                    riveArtboard = 0;
                }
                if (riveFile != 0)
                {
                    rive_File_Destroy(riveFile);
                    riveFile = 0;
                }
                if (riveScene != 0)
                {
                    rive_Scene_Destroy(riveScene);
                    riveScene = 0;
                }

                if (file != null)
                    riveFile = LoadRiveFile(file);

                if (riveFile != 0)
                {
                    riveArtboard = rive_File_GetArtboardDefault(riveFile);
                    riveScene = rive_ArtboardInstance_AnimationAt(riveArtboard, 0);
                }
                needsReload = false;
            }

            rive_Scene_AdvanceAndApply(riveScene, (float)frameClock.TimeDifference);

            rive_RenderContext_BeginFrame(riveRenderContext, renderTarget.Width, renderTarget.Height, renderTarget.MultisampleCount != MultisampleCount.None ? (int)renderTarget.MultisampleCount: 0);
            rive_Scene_Draw(riveScene, riveRenderer, size.X, size.Y);


            var nativeRenderTarget = SharpDXInterop.GetNativeResource(renderTarget) as Texture2D;
            if (nativeRenderTarget is null)
                return;
            rive_RenderTarget_D3D11_SetTargetTexture(riveRenderTarget, nativeRenderTarget.NativePointer);
            rive_RenderContext_Flush(riveRenderContext, riveRenderTarget);

            // Release render target texture
            rive_RenderTarget_D3D11_SetTargetTexture(riveRenderTarget, default);
        }

        protected override void Destroy()
        {
            if (riveRenderContext != 0)
            {
                rive_RenderContext_Destroy(riveRenderContext);
                riveRenderContext = 0;
            }

            base.Destroy();
        }

        private nint CreateRiveRenderContext(GraphicsDevice device)
        {
            var nativeDevice = SharpDXInterop.GetNativeDevice(device) as Device;
            var nativeContext = SharpDXInterop.GetNativeDeviceContext(device) as DeviceContext;
            if (nativeDevice is null || nativeContext is null)
                return default;

            return rive_RenderContext_Create_D3D11(nativeDevice.NativePointer, nativeContext.NativePointer);
        }

        private nint MakeRenderTarget(int width, int height)
        {
            return rive_RenderContext_MakeRenderTarget_D3D11(riveRenderContext, width, height);
        }

        private unsafe nint LoadRiveFile(string file)
        {
            var bytes = File.ReadAllBytes(file);
            fixed (byte* p = bytes)
            {
                return rive_File_Import(p, bytes.Length, riveRenderContext);
            }
        }
    }
}
