using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System.Runtime.CompilerServices;
using CommandList = Stride.Graphics.CommandList;
using PipelineState = Stride.Graphics.PipelineState;

namespace VL.Rive
{
    internal static class CommandListUtils
    {
        // Keep in sync with Stride.Graphics.Direct3D.PipelineState.Apply
        public static void RestorePipelineState(this CommandList commandList)
        {
            var pipelineState = commandList.currentPipelineState();
            var nativeDeviceContext = commandList.nativeDeviceContext();

            nativeDeviceContext.ComputeShader.Set(pipelineState.computeShader());
            nativeDeviceContext.VertexShader.Set(pipelineState.vertexShader());
            nativeDeviceContext.PixelShader.Set(pipelineState.pixelShader());
            nativeDeviceContext.HullShader.Set(pipelineState.hullShader());
            nativeDeviceContext.DomainShader.Set(pipelineState.domainShader());
            nativeDeviceContext.GeometryShader.Set(pipelineState.geometryShader());

            nativeDeviceContext.OutputMerger.SetBlendState(pipelineState.blendState(), nativeDeviceContext.OutputMerger.BlendFactor, pipelineState.sampleMask());

            nativeDeviceContext.Rasterizer.State = pipelineState.rasterizerState();

            nativeDeviceContext.OutputMerger.DepthStencilState = pipelineState.depthStencilState();

            nativeDeviceContext.InputAssembler.InputLayout = pipelineState.inputLayout();
            nativeDeviceContext.InputAssembler.PrimitiveTopology = pipelineState.primitiveTopology();
        }
    }

    static class D3D11Interop
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(currentPipelineState))]
        public static extern ref PipelineState currentPipelineState(this CommandList self);
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(nativeDeviceContext))]
        public static extern ref DeviceContext nativeDeviceContext(this CommandList self);
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(vertexShader))]
        public static extern ref VertexShader vertexShader(this PipelineState self);
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(geometryShader))]
        public static extern ref GeometryShader geometryShader(this PipelineState self);
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(pixelShader))]
        public static extern ref PixelShader pixelShader(this PipelineState self);
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(hullShader))]
        public static extern ref HullShader hullShader(this PipelineState self);
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(domainShader))]
        public static extern ref DomainShader domainShader(this PipelineState self);
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(computeShader))]
        public static extern ref ComputeShader computeShader(this PipelineState self);
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(blendState))]
        public static extern ref BlendState blendState(this PipelineState self);
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(sampleMask))]
        public static extern ref uint sampleMask(this PipelineState self);
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(rasterizerState))]
        public static extern ref RasterizerState rasterizerState(this PipelineState self);
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(depthStencilState))]
        public static extern ref DepthStencilState depthStencilState(this PipelineState self);
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(inputLayout))]
        public static extern ref InputLayout inputLayout(this PipelineState self);
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(primitiveTopology))]
        public static extern ref PrimitiveTopology primitiveTopology(this PipelineState self);
    }
}
