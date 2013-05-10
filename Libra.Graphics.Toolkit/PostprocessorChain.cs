#region Using

using System;
using System.Collections.ObjectModel;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class PostprocessorChain : IDisposable
    {
        #region PostprocessCollection

        public sealed class PostprocessorCollection : Collection<IPostprocessor>
        {
            internal PostprocessorCollection() { }
        }

        #endregion

        int width;

        int height;

        SurfaceFormat format;

        RenderTarget currentRenderTarget;

        RenderTarget freeRenderTarget;

        FullScreenQuad fullScreenQuad;

        DirectTextureDraw directTextureDraw;

        public Device Device { get; private set; }

        public PostprocessorCollection Postprocessors { get; private set; }

        public int Width
        {
            get { return width; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("value");

                width = value;

                InvalidateRenderTargets();
            }
        }

        public int Height
        {
            get { return height; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("value");

                height = value;

                InvalidateRenderTargets();
            }
        }

        public SurfaceFormat Format
        {
            get { return format; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("value");

                format = value;

                InvalidateRenderTargets();
            }
        }

        public PostprocessorChain(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;

            width = 1;
            height = 1;
            format = SurfaceFormat.Color;

            Postprocessors = new PostprocessorCollection();
            fullScreenQuad = new FullScreenQuad(Device);
        }

        public ShaderResourceView Draw(DeviceContext context, ShaderResourceView texture)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (texture == null) throw new ArgumentNullException("texture");

            if (Postprocessors.Count == 0)
            {
                if (directTextureDraw == null)
                    directTextureDraw = new DirectTextureDraw(Device);

                EnsureCurrentRenderTarget();

                context.SetRenderTarget(currentRenderTarget.GetRenderTargetView());

                directTextureDraw.Texture = texture;
                directTextureDraw.Apply(context);

                fullScreenQuad.Draw(context);

                context.SetRenderTarget(null);
            }
            else
            {
                var currentTexture = texture;

                for (int i = 0; i < Postprocessors.Count; i++)
                {
                    var postprocessor = Postprocessors[i];

                    if (0 < i)
                    {
                        currentTexture = currentRenderTarget.GetShaderResourceView();
                    }

                    var temp = currentRenderTarget;
                    currentRenderTarget = freeRenderTarget;
                    freeRenderTarget = temp;

                    EnsureCurrentRenderTarget();

                    context.SetRenderTarget(currentRenderTarget.GetRenderTargetView());

                    postprocessor.Texture = currentTexture;
                    postprocessor.Apply(context);

                    fullScreenQuad.Draw(context);

                    context.SetRenderTarget(null);
                }
            }

            return currentRenderTarget.GetShaderResourceView();
        }

        void EnsureCurrentRenderTarget()
        {
            if (currentRenderTarget == null)
            {
                currentRenderTarget = Device.CreateRenderTarget();
                currentRenderTarget.Width = width;
                currentRenderTarget.Height = height;
                currentRenderTarget.Format = format;
                currentRenderTarget.Initialize();
            }
        }

        void InvalidateRenderTargets()
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

        ~PostprocessorChain()
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
                if (directTextureDraw != null)
                    directTextureDraw.Dispose();

                foreach (var postprocessor in Postprocessors)
                {
                    var disposable = postprocessor as IDisposable;
                    if (disposable != null)
                        disposable.Dispose();
                }
            }

            disposed = true;
        }

        #endregion
    }
}
