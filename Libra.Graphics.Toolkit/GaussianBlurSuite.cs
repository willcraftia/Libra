#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class GaussianBlurSuite : IDisposable
    {
        GaussianBlurCore core;

        RenderTarget backingRenderTarget;

        FullScreenQuad fullScreenQuad;

        public Device Device { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public SurfaceFormat Format { get; private set; }

        public int Radius
        {
            get { return core.Radius; }
            set { core.Radius = value; }
        }

        public float Amount
        {
            get { return core.Amount; }
            set { core.Amount = value; }
        }

        public SamplerState TextureSampler
        {
            get { return core.TextureSampler; }
            set { core.TextureSampler = value; }
        }

        public GaussianBlurSuite(Device device, int width, int height, SurfaceFormat format)
        {
            if (device == null) throw new ArgumentNullException("device");
            if (width < 1) throw new ArgumentOutOfRangeException("width");
            if (height < 1) throw new ArgumentOutOfRangeException("height");

            Device = device;
            Width = width;
            Height = height;

            core = new GaussianBlurCore(Device);
            fullScreenQuad = new FullScreenQuad(Device);

            backingRenderTarget = Device.CreateRenderTarget();
            backingRenderTarget.Width = width;
            backingRenderTarget.Height = height;
            backingRenderTarget.Format = format;
            backingRenderTarget.Initialize();
        }

        public void Filter(DeviceContext context, ShaderResourceView source, RenderTargetView destination)
        {
            var previousBlendState = context.BlendState;
            var previousDepthStencilState = context.DepthStencilState;
            var previousRasterizerState = context.RasterizerState;
            var previousSamplerState = context.PixelShaderSamplers[0];

            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.None;
            context.RasterizerState = RasterizerState.CullBack;
            context.PixelShaderSamplers[0] = SamplerState.LinearClamp;

            Filter(context, source, backingRenderTarget.GetRenderTargetView(), GaussianBlurPass.Horizon);
            Filter(context, backingRenderTarget.GetShaderResourceView(), destination, GaussianBlurPass.Vertical);

            context.SetRenderTarget(null);

            // ステートを以前の状態へ戻す。
            context.BlendState = previousBlendState;
            context.DepthStencilState = previousDepthStencilState;
            context.RasterizerState = previousRasterizerState;
            context.PixelShaderSamplers[0] = previousSamplerState;
        }

        void Filter(DeviceContext context, ShaderResourceView source, RenderTargetView destination, GaussianBlurPass pass)
        {
            context.SetRenderTarget(destination);

            core.Texture = source;
            core.Pass = pass;
            core.Apply(context);

            context.PixelShaderResources[0] = source;
            
            fullScreenQuad.Draw(context);
        }

        #region IDisposable

        bool disposed;

        ~GaussianBlurSuite()
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
                core.Dispose();
                backingRenderTarget.Dispose();
                fullScreenQuad.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
