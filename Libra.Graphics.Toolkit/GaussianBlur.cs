#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class GaussianBlur : IDisposable
    {
        DeviceContext context;

        GaussianBlurShader shader;

        RenderTarget backingRenderTarget;

        SpriteBatch spriteBatch;

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

            shader = new GaussianBlurShader(context.Device);
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
            Filter(source, backingRenderTarget.GetRenderTargetView(), GaussianBlurShaderPass.Horizon);
            Filter(backingRenderTarget.GetShaderResourceView(), destination, GaussianBlurShaderPass.Vertical);

            context.SetRenderTarget(null);
        }

        void Filter(ShaderResourceView source, RenderTargetView destination, GaussianBlurShaderPass direction)
        {
            shader.Pass = direction;

            context.SetRenderTarget(destination);
            
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null, shader.Apply);
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
                shader.Dispose();
                backingRenderTarget.Dispose();
                spriteBatch.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
