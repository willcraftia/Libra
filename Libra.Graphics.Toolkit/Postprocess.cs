#region Using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class Postprocess : IDisposable
    {
        #region PassCollection

        public sealed class FilterCollection : Collection<IFilterEffect>
        {
            internal FilterCollection() { }
        }

        #endregion

        #region RenderTargetChain

        class RenderTargetChain : IDisposable
        {
            Device device;

            int width;

            int height;

            SurfaceFormat format;

            int multisampleCount;

            RenderTarget current;

            RenderTarget reserve;

            public RenderTarget Current
            {
                get
                {
                    if (current == null)
                    {
                        current = device.CreateRenderTarget();
                        current.Width = width;
                        current.Height = height;
                        current.Format = format;
                        current.MultisampleCount = multisampleCount;
                        current.Initialize();
                    }

                    return current;
                }
            }

            internal RenderTargetChain(Device device, int width, int height, SurfaceFormat format, int multisampleCount)
            {
                this.device = device;
                this.width = width;
                this.height = height;
                this.format = format;
                this.multisampleCount = multisampleCount;
            }

            internal void Swap()
            {
                var temp = current;
                current = reserve;
                reserve = temp;
            }

            #region IDisposable

            bool disposed;

            ~RenderTargetChain()
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
                    if (current != null) current.Dispose();
                    if (reserve != null) reserve.Dispose();
                }

                disposed = true;
            }

            #endregion
        }

        #endregion

        DeviceContext context;

        int width;

        int height;

        SurfaceFormat format;

        int multisampleCount;

        Dictionary<ulong, RenderTargetChain> renderTargetChains;

        SpriteBatch spriteBatch;

        public FilterCollection Filters { get; private set; }

        public int Width
        {
            get { return width; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                width = value;

                ReleaseRenderTargets();
            }
        }

        public int Height
        {
            get { return height; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                height = value;

                ReleaseRenderTargets();
            }
        }

        public SurfaceFormat Format
        {
            get { return format; }
            set
            {
                format = value;

                ReleaseRenderTargets();
            }
        }

        public int MultisampleCount
        {
            get { return multisampleCount; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                multisampleCount = value;

                ReleaseRenderTargets();
            }
        }

        public Postprocess(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.context = context;

            renderTargetChains = new Dictionary<ulong, RenderTargetChain>(4);
            Filters = new FilterCollection();
            spriteBatch = new SpriteBatch(context);

            width = 1;
            height = 1;
            format = SurfaceFormat.Color;
            multisampleCount = 1;
        }

        public ShaderResourceView Draw(ShaderResourceView texture)
        {
            if (texture == null) throw new ArgumentNullException("texture");

            var currentTexture = texture;

            int currentWidth = width;
            int currentHeight = height;

            int lastWidth = width;
            int lastHeight = height;

            RenderTargetChain renderTargetChain = null;

            for (int i = 0; i < Filters.Count; i++)
            {
                var filter = Filters[i];

                if (!filter.Enabled)
                    continue;

                lastWidth = currentWidth;
                lastHeight = currentHeight;

                var filterEffectScale = filter as IFilterEffectScale;
                if (filterEffectScale != null)
                {
                    currentWidth = (int) (currentWidth * filterEffectScale.WidthScale);
                    currentHeight = (int) (currentHeight * filterEffectScale.HeightScale);

                    if (currentWidth != lastWidth && currentHeight != lastHeight)
                    {
                        renderTargetChain = null;
                    }
                }

                if (renderTargetChain == null)
                {
                    var key = CreateRenderTargeChainKey(currentWidth, currentHeight);

                    if (!renderTargetChains.TryGetValue(key, out renderTargetChain))
                    {
                        renderTargetChain = new RenderTargetChain(context.Device, currentWidth, currentHeight, format, multisampleCount);
                        renderTargetChains[key] = renderTargetChain;
                    }
                }
                else
                {
                    renderTargetChain.Swap();
                }

                context.SetRenderTarget(renderTargetChain.Current.GetRenderTargetView());

                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, filter.Apply);
                spriteBatch.Draw(currentTexture, new Rectangle(0, 0, currentWidth, currentHeight), Color.White);
                spriteBatch.End();

                context.SetRenderTarget(null);

                currentTexture = renderTargetChain.Current.GetShaderResourceView();
            }

            return currentTexture;
        }

        ulong CreateRenderTargeChainKey(int width, int height)
        {
            uint upper = (uint) width;
            uint lower = (uint) height;
            return (((ulong) upper) << 32) | lower;
        }

        void ReleaseRenderTargets()
        {
            foreach (var renderTargetChain in renderTargetChains.Values)
            {
                renderTargetChain.Dispose();
            }

            renderTargetChains.Clear();
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

                foreach (var pass in Filters)
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
