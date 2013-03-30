#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics
{
    public interface IDevice : IDisposable
    {
        event EventHandler Disposing;

        event EventHandler BackBuffersResetting;

        event EventHandler BackBuffersReset;

        //readonly DeviceSettings Settings;

        IAdapter Adapter { get; }

        DeviceProfile Profile { get; }

        //DeviceFeatures Features { get; }

        DeviceContext ImmediateContext { get; }

        RenderTargetView BackBufferRenderTargetView { get; }

        DeviceContext CreateDeferredContext();

        VertexShader CreateVertexShader();

        PixelShader CreatePixelShader();

        InputLayout CreateInputLayout();

        ConstantBuffer CreateConstantBuffer();

        VertexBuffer CreateVertexBuffer();

        IndexBuffer CreateIndexBuffer();

        Texture2D CreateTexture2D();

        DepthStencil CreateDepthStencil();

        RenderTarget CreateRenderTarget();

        ShaderResourceView CreateShaderResourceView();

        DepthStencilView CreateDepthStencilView();

        RenderTargetView CreateRenderTargetView();

        OcclusionQuery CreateOcclusionQuery();

        int CheckMultisampleQualityLevels(SurfaceFormat format, int sampleCount);

        int CheckMultisampleQualityLevels(DepthFormat format, int sampleCount);

        void SetSwapChain(SwapChain swapChain);
    }
}
