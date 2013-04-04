#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class GaussianBlur : IDisposable
    {
        DeviceContext context;

        GaussianBlurEffect shader;

        RenderTarget backingRenderTarget;

        SpriteBatch spriteBatch;

        BlendState previousBlendState;

        SamplerState previousSamplerState;

        DepthStencilState previousDepthStencilState;

        RasterizerState previousRasterizerState;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public SurfaceFormat Format { get; private set; }

        public int Radius
        {
            get { return shader.Radius; }
            set { shader.Radius = value; }
        }

        public float Amount
        {
            get { return shader.Amount; }
            set { shader.Amount = value; }
        }

        public GaussianBlur(DeviceContext context, int width, int height, SurfaceFormat format)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (width < 1) throw new ArgumentOutOfRangeException("width");
            if (height < 1) throw new ArgumentOutOfRangeException("height");

            this.context = context;
            Width = width;
            Height = height;

            shader = new GaussianBlurEffect(context.Device);
            shader.Width = width;
            shader.Height = height;
            
            backingRenderTarget = context.Device.CreateRenderTarget();
            backingRenderTarget.Width = width;
            backingRenderTarget.Height = height;
            backingRenderTarget.Format = format;
            backingRenderTarget.Initialize();

            spriteBatch = new SpriteBatch(context);
        }

        public void Filter(ShaderResourceView source, RenderTargetView destination)
        {
            previousBlendState = context.BlendState;
            previousSamplerState = context.PixelShaderSamplers[0];
            previousDepthStencilState = context.DepthStencilState;
            previousRasterizerState = context.RasterizerState;

            Filter(source, backingRenderTarget.GetRenderTargetView(), GaussianBlurEffectPass.Horizon);
            Filter(backingRenderTarget.GetShaderResourceView(), destination, GaussianBlurEffectPass.Vertical);

            context.SetRenderTarget(null);
        }

        void Filter(ShaderResourceView source, RenderTargetView destination, GaussianBlurEffectPass direction)
        {
            shader.Pass = direction;

            context.SetRenderTarget(destination);
            
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null, shader.Apply);
            spriteBatch.Draw(source, destination.RenderTarget.Bounds, Color.White);
            spriteBatch.End();

            // SpriteBatch を暗黙的に利用していることから、
            // SpriteBatch によるステート変更を忘れがちになる。
            // このため、SpriteBatch によるステート変更前の状態へ戻す。
            context.BlendState = previousBlendState;
            context.PixelShaderSamplers[0] = previousSamplerState;
            context.DepthStencilState = previousDepthStencilState;
            context.RasterizerState = previousRasterizerState;
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
                shader.Dispose();
                backingRenderTarget.Dispose();
                spriteBatch.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
