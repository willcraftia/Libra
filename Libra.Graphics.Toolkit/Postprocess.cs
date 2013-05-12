#region Using

using System;
using System.Collections.ObjectModel;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class Postprocess : IDisposable
    {
        #region PassCollection

        public sealed class PassCollection : Collection<IPostprocessPass>
        {
            internal PassCollection() { }
        }

        #endregion

        DeviceContext context;

        int width;

        int height;

        SurfaceFormat format;

        RenderTarget currentRenderTarget;

        RenderTarget freeRenderTarget;

        SpriteBatch spriteBatch;

        public PassCollection Passes { get; private set; }

        public int Width
        {
            get { return width; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("value");

                width = value;

                ReleaseRenderTargets();
            }
        }

        public int Height
        {
            get { return height; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("value");

                height = value;

                ReleaseRenderTargets();
            }
        }

        public SurfaceFormat Format
        {
            get { return format; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("value");

                format = value;

                ReleaseRenderTargets();
            }
        }

        public Postprocess(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.context = context;

            Passes = new PassCollection();
            spriteBatch = new SpriteBatch(context);

            width = 1;
            height = 1;
            format = SurfaceFormat.Color;
        }

        public ShaderResourceView Draw(ShaderResourceView texture)
        {
            if (texture == null) throw new ArgumentNullException("texture");

            var currentTexture = texture;

            int passCount = 0;

            for (int i = 0; i < Passes.Count; i++)
            {
                var pass = Passes[i];

                if (!pass.Enabled)
                    continue;

                if (0 < passCount)
                {
                    currentTexture = currentRenderTarget.GetShaderResourceView();
                }

                var temp = currentRenderTarget;
                currentRenderTarget = freeRenderTarget;
                freeRenderTarget = temp;

                if (currentRenderTarget == null)
                {
                    currentRenderTarget = context.Device.CreateRenderTarget();
                    currentRenderTarget.Width = width;
                    currentRenderTarget.Height = height;
                    currentRenderTarget.Format = format;
                    currentRenderTarget.Initialize();
                }

                context.SetRenderTarget(currentRenderTarget.GetRenderTargetView());

                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, pass.Apply);
                spriteBatch.Draw(currentTexture, Vector2.Zero, Color.White);
                spriteBatch.End();

                context.SetRenderTarget(null);

                passCount++;
            }

            if (passCount == 0)
                return texture;

            return currentRenderTarget.GetShaderResourceView();
        }
    
        void ReleaseRenderTargets()
        {
            if (currentRenderTarget != null)
            {
                currentRenderTarget.Dispose();
                currentRenderTarget = null;
            }
            if (freeRenderTarget != null)
            {
                freeRenderTarget.Dispose();
                freeRenderTarget = null;
            }
        }

        #region IDisposable

        bool disposed;

        ~Postprocess()
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
                ReleaseRenderTargets();

                foreach (var pass in Passes)
                {
                    var disposable = pass as IDisposable;
                    if (disposable != null)
                        disposable.Dispose();
                }

                spriteBatch.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
