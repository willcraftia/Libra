#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class ShadowSceneMap : IDisposable
    {
        #region DirtyFlags

        enum DirtyFlags
        {
            RenderTarget    = (1 << 0),
            Postprocess     = (1 << 1)
        }

        #endregion

        /// <summary>
        /// シャドウ シーン マップ エフェクト。
        /// </summary>
        ShadowSceneMapEffect shadowSceneMapEffect;

        /// <summary>
        /// フル スクリーン クワッド。
        /// </summary>
        FullScreenQuad fullScreenQuad;

        /// <summary>
        /// レンダ ターゲット。
        /// </summary>
        RenderTarget renderTarget;

        int renderTargetWidth;

        int renderTargetHeight;

        int preferredRenderTargetMultisampleCount = 1;

        /// <summary>
        /// ポストプロセス。
        /// </summary>
        Postprocess postprocess;

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
        GaussianFilter blurFilter;

        /// <summary>
        /// ブラー フィルタ水平パス。
        /// </summary>
        GaussianFilterPass blurPassH;

        /// <summary>
        /// ブラー フィルタ垂直パス。
        /// </summary>
        GaussianFilterPass blurPassV;

        float blurScale = 0.25f;

        int blurRadius = 3;

        float blurSigma = 1;

        /// <summary>
        /// ダーティ フラグ。
        /// </summary>
        DirtyFlags dirtyFlags = DirtyFlags.RenderTarget | DirtyFlags.Postprocess;

        /// <summary>
        /// デバイス コンテキストを取得します。
        /// </summary>
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

        public int SplitCount
        {
            get { return shadowSceneMapEffect.SplitCount; }
            set { shadowSceneMapEffect.SplitCount = value; }
        }

        public float DepthBias
        {
            get { return shadowSceneMapEffect.DepthBias; }
            set { shadowSceneMapEffect.DepthBias = value; }
        }

        public ShadowMapForm ShadowMapForm
        {
            get { return shadowSceneMapEffect.ShadowMapForm; }
            set { shadowSceneMapEffect.ShadowMapForm = value; }
        }

        public bool PcfEnabled
        {
            get { return shadowSceneMapEffect.PcfEnabled; }
            set { shadowSceneMapEffect.PcfEnabled = value; }
        }

        public int PcfRadius
        {
            get { return shadowSceneMapEffect.PcfRadius; }
            set { shadowSceneMapEffect.PcfRadius = value; }
        }

        public Matrix View
        {
            get { return shadowSceneMapEffect.View; }
            set { shadowSceneMapEffect.View = value; }
        }

        public Matrix Projection
        {
            get { return shadowSceneMapEffect.Projection; }
            set { shadowSceneMapEffect.Projection = value; }
        }

        public ShaderResourceView LinearDepthMap
        {
            get { return shadowSceneMapEffect.LinearDepthMap; }
            set { shadowSceneMapEffect.LinearDepthMap = value; }
        }

        public SamplerState LinearDepthMapSampler
        {
            get { return shadowSceneMapEffect.LinearDepthMapSampler; }
            set { shadowSceneMapEffect.LinearDepthMapSampler = value; }
        }

        public SamplerState ShadowMapSampler
        {
            get { return shadowSceneMapEffect.ShadowMapSampler; }
            set { shadowSceneMapEffect.ShadowMapSampler = value; }
        }

        public bool BlurEnabled { get; set; }

        public float BlurScale
        {
            get { return blurScale; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                if (blurScale == value)
                    return;

                blurScale = value;

                dirtyFlags |= DirtyFlags.Postprocess;
            }
        }

        public int BlurRadius
        {
            get { return blurRadius; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                blurRadius = value;

                dirtyFlags |= DirtyFlags.Postprocess;
            }
        }

        public float BlurSigma
        {
            get { return blurSigma; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                blurSigma = value;

                dirtyFlags |= DirtyFlags.Postprocess;
            }
        }

        public ShaderResourceView BaseTexture { get; private set; }

        public ShaderResourceView FinalTexture { get; private set; }

        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="deviceContext">デバイス コンテキスト。</param>
        public ShadowSceneMap(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            shadowSceneMapEffect = new ShadowSceneMapEffect(deviceContext);

            fullScreenQuad = new FullScreenQuad(deviceContext);
            fullScreenQuad.ViewRayEnabled = true;

            postprocess = new Postprocess(deviceContext);
            postprocess.Format = SurfaceFormat.Single;
        }

        public float GetSplitDistance(int index)
        {
            if ((uint) CascadeShadowMap.MaxSplitCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            return shadowSceneMapEffect.GetSplitDistance(index);
        }

        public void SetSplitDistance(int index, float value)
        {
            if ((uint) CascadeShadowMap.MaxSplitCount + 1 < (uint) index) throw new ArgumentOutOfRangeException("index");

            shadowSceneMapEffect.SetSplitDistance(index, value);
        }

        public Matrix GetLightViewProjection(int index)
        {
            if ((uint) CascadeShadowMap.MaxSplitCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            return shadowSceneMapEffect.GetLightViewProjection(index);
        }

        public void SetLightViewProjection(int index, Matrix value)
        {
            if ((uint) CascadeShadowMap.MaxSplitCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            shadowSceneMapEffect.SetLightViewProjection(index, value);
        }

        public ShaderResourceView GetShadowMap(int index)
        {
            if ((uint) CascadeShadowMap.MaxSplitCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            return shadowSceneMapEffect.GetShadowMap(index);
        }

        public void SetShadowMap(int index, ShaderResourceView value)
        {
            if ((uint) CascadeShadowMap.MaxSplitCount < (uint) index) throw new ArgumentOutOfRangeException("index");

            shadowSceneMapEffect.SetShadowMap(index, value);
        }

        public void UpdateShadowMapSettings(CascadeShadowMap cascadeShadowMap)
        {
            if (cascadeShadowMap == null) throw new ArgumentNullException("cascadeShadowMap");

            SplitCount = cascadeShadowMap.SplitCount;
            ShadowMapForm = cascadeShadowMap.ShadowMapForm;

            int i = 0;
            for (; i < CascadeShadowMap.MaxSplitCount; i++)
            {
                SetSplitDistance(i, cascadeShadowMap.GetSplitDistance(i));
                SetLightViewProjection(i, cascadeShadowMap.GetLightViewProjection(i));
                SetShadowMap(i, cascadeShadowMap.GetTexture(i));
            }
            SetSplitDistance(i, cascadeShadowMap.GetSplitDistance(i));
        }

        public void Draw()
        {
            PrepareRenderTarget();
            DrawShadow();
            PreparePostprocess();
            ApplyPostprocess();
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

                postprocess.Width = renderTarget.Width;
                postprocess.Height = renderTarget.Height;
                postprocess.PreferredMultisampleCount = preferredRenderTargetMultisampleCount;

                dirtyFlags &= ~DirtyFlags.RenderTarget;
            }
        }

        void DrawShadow()
        {
            DeviceContext.DepthStencilState = DepthStencilState.None;
            DeviceContext.SetRenderTarget(renderTarget);
            DeviceContext.Clear(Vector4.Zero);

            shadowSceneMapEffect.Apply();

            fullScreenQuad.Projection = shadowSceneMapEffect.Projection;
            fullScreenQuad.Draw();

            DeviceContext.SetRenderTarget(null);

            DeviceContext.DepthStencilState = null;

            BaseTexture = renderTarget;
        }

        void PreparePostprocess()
        {
            if (BlurEnabled && (dirtyFlags & DirtyFlags.Postprocess) != 0)
            {
                postprocess.Filters.Clear();

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

                    postprocess.Filters.Add(downFilter);
                }

                if (blurFilter == null)
                {
                    blurFilter = new GaussianFilter(DeviceContext);
                    blurPassH = new GaussianFilterPass(blurFilter, GaussianFilterDirection.Horizon);
                    blurPassV = new GaussianFilterPass(blurFilter, GaussianFilterDirection.Vertical);
                }

                blurFilter.Radius = blurRadius;
                blurFilter.Sigma = blurSigma;

                postprocess.Filters.Add(blurPassH);
                postprocess.Filters.Add(blurPassV);

                if (blurScale != 1.0f)
                {
                    postprocess.Filters.Add(upFilter);
                }

                dirtyFlags &= ~DirtyFlags.Postprocess;
            }
        }

        void ApplyPostprocess()
        {
            if (BlurEnabled)
            {
                FinalTexture = postprocess.Draw(BaseTexture);
            }
            else
            {
                FinalTexture = BaseTexture;
            }
        }

        #region IDisposable

        bool disposed;

        ~ShadowSceneMap()
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
                shadowSceneMapEffect.Dispose();
                fullScreenQuad.Dispose();
                postprocess.Dispose();

                if (renderTarget != null) renderTarget.Dispose();
                if (downFilter != null) downFilter.Dispose();
                if (upFilter != null) upFilter.Dispose();
                if (blurFilter != null) blurFilter.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
