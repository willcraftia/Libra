#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class GaussianBlur : IDisposable
    {
        DeviceContext context;

        GaussianBlurEffect effect;

        RenderTarget backingRenderTarget;

        SpriteBatch spriteBatch;

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

        public bool Enabled { get; set; }

        public GaussianBlur(DeviceContext context, int width, int height, SurfaceFormat format)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (width < 1) throw new ArgumentOutOfRangeException("width");
            if (height < 1) throw new ArgumentOutOfRangeException("height");

            this.context = context;
            Width = width;
            Height = height;

            effect = new GaussianBlurEffect(context.Device);
            effect.Width = width;
            effect.Height = height;
            
            backingRenderTarget = context.Device.CreateRenderTarget();
            backingRenderTarget.Width = width;
            backingRenderTarget.Height = height;
            backingRenderTarget.Format = format;
            backingRenderTarget.Initialize();

            spriteBatch = new SpriteBatch(context);

            Enabled = true;
        }

        public void Filter(ShaderResourceView source, RenderTargetView destination)
        {
            Filter(source, backingRenderTarget.GetRenderTargetView(), GaussianBlurEffectPass.Horizon);
            Filter(backingRenderTarget.GetShaderResourceView(), destination, GaussianBlurEffectPass.Vertical);

            context.SetRenderTarget(null);
        }

        void Filter(ShaderResourceView source, RenderTargetView destination, GaussianBlurEffectPass direction)
        {
            effect.Pass = direction;

            context.SetRenderTarget(destination);

            Action<DeviceContext> applyShader = null;
            if (Enabled) applyShader = effect.Apply;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null, applyShader);
            spriteBatch.Draw(source, destination.RenderTarget.Bounds, Color.White);
            spriteBatch.End();
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
                spriteBatch.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
