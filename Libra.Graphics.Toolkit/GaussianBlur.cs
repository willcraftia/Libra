#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class GaussianBlur : IDisposable
    {
        GaussianBlurEffect effect;

        RenderTarget backingRenderTarget;

        FullScreenQuad fullScreenQuad;

        public Device Device { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public SurfaceFormat Format { get; private set; }

        public int Radius
        {
            get { return effect.Radius; }
            set { effect.Radius = value; }
        }

        public float Amount
        {
            get { return effect.Amount; }
            set { effect.Amount = value; }
        }

        public GaussianBlur(Device device, int width, int height, SurfaceFormat format)
        {
            if (device == null) throw new ArgumentNullException("device");
            if (width < 1) throw new ArgumentOutOfRangeException("width");
            if (height < 1) throw new ArgumentOutOfRangeException("height");

            Device = device;
            Width = width;
            Height = height;

            effect = new GaussianBlurEffect(Device);
            effect.Width = width;
            effect.Height = height;

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

            Filter(context, source, backingRenderTarget.GetRenderTargetView(), GaussianBlurEffectPass.Horizon);
            Filter(context, backingRenderTarget.GetShaderResourceView(), destination, GaussianBlurEffectPass.Vertical);

            context.SetRenderTarget(null);

            // ステートを以前の状態へ戻す。
            context.BlendState = previousBlendState;
            context.DepthStencilState = previousDepthStencilState;
            context.RasterizerState = previousRasterizerState;
            context.PixelShaderSamplers[0] = previousSamplerState;
        }

        void Filter(DeviceContext context, ShaderResourceView source, RenderTargetView destination, GaussianBlurEffectPass direction)
        {
            context.SetRenderTarget(destination);

            effect.Pass = direction;
            effect.Apply(context);

            context.PixelShaderResources[0] = source;
            
            fullScreenQuad.Draw(context);
        }

        #region IDisposable

        bool disposed;

        ~GaussianBlur()
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
                effect.Dispose();
                backingRenderTarget.Dispose();
                fullScreenQuad.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
