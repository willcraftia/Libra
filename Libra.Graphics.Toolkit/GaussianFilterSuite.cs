#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class GaussianFilterSuite : IDisposable
    {
        DeviceContext DeviceContext;

        FullScreenQuad fullScreenQuad;

        GaussianFilter gaussianFilter;

        RenderTarget backingRenderTarget;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public SurfaceFormat Format { get; private set; }

        public int Radius
        {
            get { return gaussianFilter.Radius; }
            set { gaussianFilter.Radius = value; }
        }

        public float Sigma
        {
            get { return gaussianFilter.Sigma; }
            set { gaussianFilter.Sigma = value; }
        }

        public GaussianFilterSuite(DeviceContext deviceContext, int width, int height, SurfaceFormat format)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");
            if (width < 1) throw new ArgumentOutOfRangeException("width");
            if (height < 1) throw new ArgumentOutOfRangeException("height");

            DeviceContext = deviceContext;
            Width = width;
            Height = height;

            fullScreenQuad = new FullScreenQuad(deviceContext);

            gaussianFilter = new GaussianFilter(deviceContext);

            backingRenderTarget = deviceContext.Device.CreateRenderTarget();
            backingRenderTarget.Width = width;
            backingRenderTarget.Height = height;
            backingRenderTarget.Format = format;
            backingRenderTarget.Initialize();
        }

        public void Filter(ShaderResourceView source, RenderTargetView destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");

            var previousBlendState = DeviceContext.BlendState;
            var previousDepthStencilState = DeviceContext.DepthStencilState;
            var previousRasterizerState = DeviceContext.RasterizerState;
            var previousSamplerState = DeviceContext.PixelShaderSamplers[0];

            DeviceContext.BlendState = BlendState.Opaque;
            DeviceContext.DepthStencilState = DepthStencilState.None;
            DeviceContext.RasterizerState = RasterizerState.CullBack;
            DeviceContext.PixelShaderSamplers[0] = SamplerState.LinearClamp;

            Filter(source, backingRenderTarget, GaussianFilterDirection.Horizon);
            Filter(backingRenderTarget, destination, GaussianFilterDirection.Vertical);

            DeviceContext.SetRenderTarget(null);

            // ステートを以前の状態へ戻す。
            DeviceContext.BlendState = previousBlendState;
            DeviceContext.DepthStencilState = previousDepthStencilState;
            DeviceContext.RasterizerState = previousRasterizerState;
            DeviceContext.PixelShaderSamplers[0] = previousSamplerState;
        }

        void Filter(ShaderResourceView source, RenderTargetView destination, GaussianFilterDirection direction)
        {
            DeviceContext.SetRenderTarget(destination);

            gaussianFilter.Direction = direction;
            gaussianFilter.Apply();

            DeviceContext.PixelShaderResources[0] = source;

            fullScreenQuad.Draw();
        }

        #region IDisposable

        bool disposed;

        ~GaussianFilterSuite()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                fullScreenQuad.Dispose();
                gaussianFilter.Dispose();
                backingRenderTarget.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
