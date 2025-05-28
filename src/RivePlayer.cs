using SharpDX.Direct3D11;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VL.Core.Import;
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

            if (riveRenderer == 0)
            {
                riveRenderer = rive_Renderer_Create(riveRenderContext);
                riveRenderTarget = MakeRenderTarget(renderTarget);
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
                    riveScene = rive_ArtboardInstance_StaticScene(riveArtboard);
                    rive_Scene_AdvanceAndApply(riveScene, 0f);
                }
                needsReload = false;
            }

            rive_RenderContext_BeginFrame(riveRenderContext, renderTarget.Width, renderTarget.Height, renderTarget.MultisampleCount != MultisampleCount.None ? (int)renderTarget.MultisampleCount: 0);
            rive_Scene_Draw(riveScene, riveRenderer);
            rive_RenderContext_Flush(riveRenderContext, riveRenderTarget);
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

        private nint MakeRenderTarget(Texture renderTarget)
        {
            var nativeRenderTarget = SharpDXInterop.GetNativeResource(renderTarget) as Texture2D;
            if (nativeRenderTarget is null)
                return default;

            var riveRenderTarget = rive_RenderContext_MakeRenderTarget_D3D11(riveRenderContext, renderTarget.Width, renderTarget.Height);
            rive_RenderTarget_D3D11_SetTargetTexture(riveRenderTarget, nativeRenderTarget.NativePointer);
            return riveRenderTarget;
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
