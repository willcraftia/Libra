#region Using

using System;
using System.Collections.ObjectModel;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class PostprocessChain : IDisposable
    {
        #region PostprocessCollection

        public sealed class PostprocessCollection : Collection<IPostprocess>
        {
            internal PostprocessCollection() { }
        }

        #endregion

        DeviceContext context;

        int width;

        int height;

        SurfaceFormat format;

        RenderTarget currentRenderTarget;

        RenderTarget freeRenderTarget;

        SpriteBatch spriteBatch;

        public PostprocessCollection Postprocesses { get; private set; }

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

        public PostprocessChain(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.context = context;

            Postprocesses = new PostprocessCollection();
            spriteBatch = new SpriteBatch(context);

            width = 1;
            height = 1;
            format = SurfaceFormat.Color;
        }

        public ShaderResourceView Draw(ShaderResourceView texture)
        {
            if (texture == null) throw new ArgumentNullException("texture");

            var currentTexture = texture;

            int processCount = 0;

            for (int i = 0; i < Postprocesses.Count; i++)
            {
                var postprocess = Postprocesses[i];

                if (!postprocess.Enabled)
                    continue;

                if (0 < processCount)
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

                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, postprocess.Apply);
                spriteBatch.Draw(currentTexture, Vector2.Zero, Color.White);
                spriteBatch.End();

                context.SetRenderTarget(null);

                processCount++;
            }

            if (processCount == 0)
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

        ~PostprocessChain()
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

                foreach (var postprocessor in Postprocesses)
                {
                    var disposable = postprocessor as IDisposable;
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
