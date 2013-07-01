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

        DeviceContext context;

        int width;

        int height;

        SurfaceFormat format;

        int multisampleCount;

        Dictionary<ulong, RenderTargetChain> renderTargetChains;

        FullScreenQuad fullScreenQuad;

        Matrix projection;

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

        public Matrix Projection
        {
            get { return projection; }
            set { projection = value; }
        }

        public bool Enabled { get; set; }

        public Postprocess(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.context = context;

            renderTargetChains = new Dictionary<ulong, RenderTargetChain>(4);
            Filters = new FilterCollection();
            fullScreenQuad = new FullScreenQuad(context);

            width = 1;
            height = 1;
            format = SurfaceFormat.Color;
            multisampleCount = 1;
            projection = Matrix.Identity;

            Enabled = true;
        }

        public ShaderResourceView Draw(ShaderResourceView texture)
        {
            if (texture == null) throw new ArgumentNullException("texture");

            if (!Enabled)
                return texture;

            var currentTexture = texture;

            int currentWidth = width;
            int currentHeight = height;

            int lastWidth = width;
            int lastHeight = height;

            var previousBlendState = context.BlendState;
            var previousDepthStencilState = context.DepthStencilState;
            var previousRasterizerState = context.RasterizerState;
            var previousSamplerState = context.PixelShaderSamplers[0];

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
                        renderTargetChain = new RenderTargetChain(context.Device);
                        renderTargetChain.Width = currentWidth;
                        renderTargetChain.Height = currentHeight;
                        renderTargetChain.Format = format;
                        renderTargetChain.PreferredMultisampleCount = multisampleCount;

                        renderTargetChains[key] = renderTargetChain;
                    }
                }
                else
                {
                    renderTargetChain.Next();
                }

                context.SetRenderTarget(renderTargetChain.Current);

                context.BlendState = BlendState.Opaque;
                context.DepthStencilState = DepthStencilState.None;
                context.RasterizerState = RasterizerState.CullBack;

                filter.Texture = currentTexture;
                filter.Apply(context);

                if (Attribute.IsDefined(filter.GetType(), typeof(ViewRayRequiredAttribute)))
                {
                    fullScreenQuad.ViewRayEnabled = true;
                    fullScreenQuad.Projection = projection;
                }

                fullScreenQuad.Draw();
                fullScreenQuad.ViewRayEnabled = false;

                context.SetRenderTarget(null);

                currentTexture = renderTargetChain.Current;
            }

            // ステートを以前の状態へ戻す。
            context.BlendState = previousBlendState;
            context.DepthStencilState = previousDepthStencilState;
            context.RasterizerState = previousRasterizerState;
            context.PixelShaderSamplers[0] = previousSamplerState;

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

                fullScreenQuad.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
