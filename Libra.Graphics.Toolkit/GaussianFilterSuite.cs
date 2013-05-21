#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class GaussianFilterSuite : IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public FullScreenQuad FullScreenQuad { get; private set; }

            public SharedDeviceResource(Device device)
            {
                FullScreenQuad = new FullScreenQuad(device);
            }
        }

        #endregion

        DeviceContext context;

        SharedDeviceResource sharedDeviceResource;

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

        public GaussianFilterSuite(DeviceContext context, int width, int height, SurfaceFormat format)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (width < 1) throw new ArgumentOutOfRangeException("width");
            if (height < 1) throw new ArgumentOutOfRangeException("height");

            this.context = context;
            Width = width;
            Height = height;

            sharedDeviceResource = context.Device.GetSharedResource<GaussianFilterSuite, SharedDeviceResource>();

            gaussianFilter = new GaussianFilter(context.Device);

            backingRenderTarget = context.Device.CreateRenderTarget();
            backingRenderTarget.Width = width;
            backingRenderTarget.Height = height;
            backingRenderTarget.Format = format;
            backingRenderTarget.Initialize();
        }

        public void Filter(ShaderResourceView source, RenderTargetView destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");

            var previousBlendState = context.BlendState;
            var previousDepthStencilState = context.DepthStencilState;
            var previousRasterizerState = context.RasterizerState;
            var previousSamplerState = context.PixelShaderSamplers[0];

            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.None;
            context.RasterizerState = RasterizerState.CullBack;
            context.PixelShaderSamplers[0] = SamplerState.LinearClamp;

            Filter(source, backingRenderTarget, GaussianFilterDirection.Horizon);
            Filter(backingRenderTarget, destination, GaussianFilterDirection.Vertical);

            context.SetRenderTarget(null);

            // ステートを以前の状態へ戻す。
            context.BlendState = previousBlendState;
            context.DepthStencilState = previousDepthStencilState;
            context.RasterizerState = previousRasterizerState;
            context.PixelShaderSamplers[0] = previousSamplerState;
        }

        void Filter(ShaderResourceView source, RenderTargetView destination, GaussianFilterDirection direction)
        {
            context.SetRenderTarget(destination);

            gaussianFilter.Direction = direction;
            gaussianFilter.Apply(context);

            context.PixelShaderResources[0] = source;

            sharedDeviceResource.FullScreenQuad.Draw(context);
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
                sharedDeviceResource = null;
                gaussianFilter.Dispose();
                backingRenderTarget.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
