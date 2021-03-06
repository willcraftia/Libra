﻿#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class SSAOMap : IDisposable
    {
        #region DirtyFlags

        enum DirtyFlags
        {
            RenderTarget    = (1 << 0),
            FilterChain     = (1 << 1)
        }

        #endregion

        /// <summary>
        /// 環境光閉塞マップ エフェクト。
        /// </summary>
        SSAOMapEffect ssaoMapEffect;

        /// <summary>
        /// フルスクリーン クワッド。
        /// </summary>
        FullScreenQuad fullScreenQuad;

        /// <summary>
        /// レンダ ターゲット。
        /// </summary>
        RenderTarget renderTarget;

        /// <summary>
        /// フィルタ チェーン。
        /// </summary>
        FilterChain filterChain;

        /// <summary>
        /// ダウン サンプリング フィルタ。
        /// </summary>
        DownFilter downFilter;

        /// <summary>
        /// アップ サンプリング フィルタ。
        /// </summary>
        UpFilter upFilter;

        /// <summary>
        /// ブラー フィルタ。
        /// </summary>
        NormalDepthBilateralFilter blurFilter;

        /// <summary>
        /// ブラー フィルタ 水平パス。
        /// </summary>
        GaussianFilterPass blurPassH;

        /// <summary>
        /// ブラー フィルタ 垂直パス。
        /// </summary>
        GaussianFilterPass blurPassV;

        int renderTargetWidth = 1;

        int renderTargetHeight = 1;

        int preferredRenderTargetMultisampleCount = 1;

        float blurScale = 1.0f;

        int blurIteration = 3;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public int RenderTargetWidth
        {
            get { return renderTargetWidth; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                if (renderTargetWidth == value)
                    return;

                renderTargetWidth = value;

                dirtyFlags |= DirtyFlags.RenderTarget;
            }
        }

        public int RenderTargetHeight
        {
            get { return renderTargetHeight; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                if (renderTargetHeight == value)
                    return;

                renderTargetHeight = value;

                dirtyFlags |= DirtyFlags.RenderTarget;
            }
        }

        public int PreferredRenderTargetMultisampleCount
        {
            get { return preferredRenderTargetMultisampleCount; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                if (preferredRenderTargetMultisampleCount == value)
                    return;

                preferredRenderTargetMultisampleCount = value;

                dirtyFlags |= DirtyFlags.RenderTarget;
            }
        }

        public int Seed
        {
            get { return ssaoMapEffect.Seed; }
            set { ssaoMapEffect.Seed = value; }
        }

        public float Strength
        {
            get { return ssaoMapEffect.Strength; }
            set { ssaoMapEffect.Strength = value; }
        }

        public float Attenuation
        {
            get { return ssaoMapEffect.Attenuation; }
            set { ssaoMapEffect.Attenuation = value; }
        }

        public float Radius
        {
            get { return ssaoMapEffect.Radius; }
            set { ssaoMapEffect.Radius = value; }
        }

        public int SampleCount
        {
            get { return ssaoMapEffect.SampleCount; }
            set { ssaoMapEffect.SampleCount = value; }
        }

        public Matrix Projection
        {
            get { return ssaoMapEffect.Projection; }
            set { ssaoMapEffect.Projection = value; }
        }

        public float BlurScale
        {
            get { return blurScale; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                if (blurScale == value)
                    return;

                blurScale = value;

                dirtyFlags |= DirtyFlags.FilterChain;
            }
        }

        public int BlurIteration
        {
            get { return blurIteration; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value");

                if (blurIteration == value)
                    return;

                blurIteration = value;

                dirtyFlags |= DirtyFlags.FilterChain;
            }
        }

        public int BlurRadius
        {
            get { return blurFilter.Radius; }
            set { blurFilter.Radius = value; }
        }

        public float BlurSpaceSigma
        {
            get { return blurFilter.SpaceSigma; }
            set { blurFilter.SpaceSigma = value; }
        }

        public float BlurDepthSigma
        {
            get { return blurFilter.DepthSigma; }
            set { blurFilter.DepthSigma = value; }
        }

        public float BlurNormalSigma
        {
            get { return blurFilter.NormalSigma; }
            set { blurFilter.NormalSigma = value; }
        }

        public ShaderResourceView LinearDepthMap
        {
            get { return ssaoMapEffect.LinearDepthMap; }
            set { ssaoMapEffect.LinearDepthMap = value; }
        }

        public SamplerState LinearDepthMapSampler
        {
            get { return ssaoMapEffect.LinearDepthMapSampler; }
            set { ssaoMapEffect.LinearDepthMapSampler = value; }
        }

        public ShaderResourceView NormalMap
        {
            get { return ssaoMapEffect.NormalMap; }
            set { ssaoMapEffect.NormalMap = value; }
        }

        public SamplerState NormalMapSampler
        {
            get { return ssaoMapEffect.NormalMapSampler; }
            set { ssaoMapEffect.NormalMapSampler = value; }
        }

        public ShaderResourceView BaseTexture { get; private set; }

        public ShaderResourceView FinalTexture { get; private set; }

        public SSAOMap(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            ssaoMapEffect = new SSAOMapEffect(deviceContext);

            fullScreenQuad = new FullScreenQuad(deviceContext);
            fullScreenQuad.ViewRayEnabled = true;

            filterChain = new FilterChain(DeviceContext);
            filterChain.Format = SurfaceFormat.Single;

            blurFilter = new NormalDepthBilateralFilter(DeviceContext);
            blurPassH = new GaussianFilterPass(blurFilter, GaussianFilterDirection.Horizon);
            blurPassV = new GaussianFilterPass(blurFilter, GaussianFilterDirection.Vertical);

            dirtyFlags = DirtyFlags.RenderTarget | DirtyFlags.FilterChain;
        }

        public void Draw()
        {
            PrepareRenderTarget();
            DrawOcclusion();
            PrepareFilterChain();
            ApplyFilterChain();
        }

        void PrepareRenderTarget()
        {
            if ((dirtyFlags & DirtyFlags.RenderTarget) != 0)
            {
                if (renderTarget != null)
                    renderTarget.Dispose();

                renderTarget = DeviceContext.Device.CreateRenderTarget();
                renderTarget.Width = renderTargetWidth;
                renderTarget.Height = renderTargetHeight;
                renderTarget.Format = SurfaceFormat.Single;
                renderTarget.PreferredMultisampleCount = preferredRenderTargetMultisampleCount;
                renderTarget.Initialize();

                filterChain.Width = renderTarget.Width;
                filterChain.Height = renderTarget.Height;
                filterChain.PreferredMultisampleCount = preferredRenderTargetMultisampleCount;

                dirtyFlags &= ~DirtyFlags.RenderTarget;
            }
        }

        void DrawOcclusion()
        {
            DeviceContext.DepthStencilState = DepthStencilState.None;
            DeviceContext.SetRenderTarget(renderTarget);
            DeviceContext.Clear(Vector4.One);

            ssaoMapEffect.Apply();

            fullScreenQuad.Projection = ssaoMapEffect.Projection;
            fullScreenQuad.Draw();

            DeviceContext.SetRenderTarget(null);

            DeviceContext.DepthStencilState = null;

            BaseTexture = renderTarget;
        }

        void PrepareFilterChain()
        {
            if ((dirtyFlags & DirtyFlags.FilterChain) != 0)
            {
                filterChain.Filters.Clear();

                if (blurScale != 1.0f)
                {
                    if (downFilter == null)
                        downFilter = new DownFilter(DeviceContext);

                    if (upFilter == null)
                        upFilter = new UpFilter(DeviceContext);

                    downFilter.WidthScale = blurScale;
                    downFilter.HeightScale = blurScale;

                    var upScale = 1.0f / blurScale;
                    upFilter.WidthScale = upScale;
                    upFilter.HeightScale = upScale;

                    filterChain.Filters.Add(downFilter);
                }

                for (int i = 0; i < blurIteration; i++)
                {
                    filterChain.Filters.Add(blurPassH);
                    filterChain.Filters.Add(blurPassV);
                }

                if (blurScale != 1.0f)
                {
                    filterChain.Filters.Add(upFilter);
                }

                dirtyFlags &= ~DirtyFlags.FilterChain;
            }
        }

        void ApplyFilterChain()
        {
            blurFilter.LinearDepthMap = ssaoMapEffect.LinearDepthMap;
            blurFilter.LinearDepthMapSampler = ssaoMapEffect.LinearDepthMapSampler;
            blurFilter.NormalMap = ssaoMapEffect.NormalMap;
            blurFilter.NormalMapSampler = ssaoMapEffect.NormalMapSampler;

            FinalTexture = filterChain.Draw(BaseTexture);
        }

        #region IDisposable

        bool disposed;

        ~SSAOMap()
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
                ssaoMapEffect.Dispose();
                fullScreenQuad.Dispose();
                filterChain.Dispose();
                blurFilter.Dispose();

                if (renderTarget != null) renderTarget.Dispose();
                if (downFilter != null) downFilter.Dispose();
                if (upFilter != null) upFilter.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
