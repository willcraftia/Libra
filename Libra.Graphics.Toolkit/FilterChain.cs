#region Using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class FilterChain : IDisposable
    {
        #region FilterCollection

        public sealed class FilterCollection : Collection<IFilterEffect>
        {
            internal FilterCollection() { }
        }

        #endregion

        int width = 1;

        int height = 1;

        SurfaceFormat format = SurfaceFormat.Color;

        int preferredMultisampleCount = 1;

        Dictionary<ulong, RenderTargetChain> renderTargetChains = new Dictionary<ulong, RenderTargetChain>(4);

        FullScreenQuad fullScreenQuad;

        Matrix projection = Matrix.Identity;

        public DeviceContext DeviceContext { get; private set; }

        public FilterCollection Filters { get; private set; }

        public int Width
        {
            get { return width; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                if (width == value) return;

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

                if (height == value) return;

                height = value;

                ReleaseRenderTargets();
            }
        }

        public SurfaceFormat Format
        {
            get { return format; }
            set
            {
                if (format == value) return;

                format = value;

                ReleaseRenderTargets();
            }
        }

        public int PreferredMultisampleCount
        {
            get { return preferredMultisampleCount; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                if (preferredMultisampleCount == value) return;

                preferredMultisampleCount = value;

                ReleaseRenderTargets();
            }
        }

        public Matrix Projection
        {
            get { return projection; }
            set { projection = value; }
        }

        public bool Enabled { get; set; }

        public FilterChain(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            DeviceContext = context;

            Filters = new FilterCollection();
            fullScreenQuad = new FullScreenQuad(context);

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

            var previousBlendState = DeviceContext.BlendState;
            var previousDepthStencilState = DeviceContext.DepthStencilState;
            var previousRasterizerState = DeviceContext.RasterizerState;
            var previousSamplerState = DeviceContext.PixelShaderSamplers[0];

            DeviceContext.BlendState = null;
            DeviceContext.RasterizerState = null;
            DeviceContext.DepthStencilState = DepthStencilState.None;

            RenderTargetChain renderTargetChain = null;

            foreach (var chain in renderTargetChains.Values)
                chain.Reset();

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
                        renderTargetChain = new RenderTargetChain(DeviceContext.Device);
                        renderTargetChain.Width = currentWidth;
                        renderTargetChain.Height = currentHeight;
                        renderTargetChain.Format = format;
                        renderTargetChain.PreferredMultisampleCount = preferredMultisampleCount;

                        renderTargetChains[key] = renderTargetChain;
                    }
                }

                DeviceContext.SetRenderTarget(renderTargetChain.Current);

                filter.Texture = currentTexture;
                filter.Apply();

                if (Attribute.IsDefined(filter.GetType(), typeof(ViewRayRequiredAttribute)))
                {
                    fullScreenQuad.ViewRayEnabled = true;
                    fullScreenQuad.Projection = projection;
                }

                fullScreenQuad.Draw();
                fullScreenQuad.ViewRayEnabled = false;

                DeviceContext.SetRenderTarget(null);

                currentTexture = renderTargetChain.Current;

                renderTargetChain.Next();
            }

            // ステートを以前の状態へ戻す。
            DeviceContext.BlendState = previousBlendState;
            DeviceContext.DepthStencilState = previousDepthStencilState;
            DeviceContext.RasterizerState = previousRasterizerState;
            DeviceContext.PixelShaderSamplers[0] = previousSamplerState;

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

        ~FilterChain()
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
