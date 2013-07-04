#region Using

using System;
using System.IO;

#endregion

namespace Libra.Graphics
{
    public abstract class RenderTarget : Texture2D
    {
        SurfaceFormat depthStencilFormat;

        RenderTargetView renderTargetView;

        public bool IsBackBuffer { get; private set; }

        public SurfaceFormat DepthStencilFormat
        {
            get { return depthStencilFormat; }
            set
            {
                AssertNotInitialized();

                depthStencilFormat = value;
            }
        }

        public bool DepthStencilEnabled { get; set; }

        public RenderTargetUsage RenderTargetUsage { get; set; }

        public DepthStencil DepthStencil { get; private set; }

        protected RenderTarget(Device device, bool isBackBuffer)
            : base(device)
        {
            IsBackBuffer = isBackBuffer;

            depthStencilFormat = SurfaceFormat.Depth24Stencil8;
            RenderTargetUsage = RenderTargetUsage.Discard;
        }

        // バック バッファ用初期化メソッド。
        public void Initialize(SwapChain swapChain, int index)
        {
            AssertNotInitialized();
            if (swapChain == null) throw new ArgumentNullException("swapChain");
            if (index < 0) throw new ArgumentOutOfRangeException("index");

            InitializeRenderTarget(swapChain, index);

            if (DepthStencilEnabled)
                DepthStencil = InitializeDepthStencil();

            Initialized = true;
        }

        /// <summary>
        /// 暗黙的に GetRenderTargetView() を呼び出して RenderTargetView 型とします。
        /// </summary>
        /// <param name="renderTarget">RenderTarget。</param>
        /// <returns>RenderTarget 内部で管理する RenderTargetView。</returns>
        public static implicit operator RenderTargetView(RenderTarget renderTarget)
        {
            if (renderTarget == null) return null;

            return renderTarget.GetRenderTargetView();
        }

        public RenderTargetView GetRenderTargetView()
        {
            if (renderTargetView == null)
            {
                renderTargetView = Device.CreateRenderTargetView();
                renderTargetView.Initialize(this);
            }
            return renderTargetView;
        }

        protected sealed override void InitializeCore()
        {
            // 深度ステンシル フォーマットについて最適なマルチサンプリング設定を検出。
            int depthStencilMultisampleCount = 1;
            int depthStencilMultisampleQuality = 0;
            for (int i = PreferredMultisampleCount; 1 < i; i /= 2)
            {
                var multisampleQualityLevels = Device.CheckMultisampleQualityLevels(depthStencilFormat, i);
                if (0 < multisampleQualityLevels)
                {
                    depthStencilMultisampleCount = i;
                    depthStencilMultisampleQuality = multisampleQualityLevels - 1;
                    break;
                }
            }

            if (depthStencilMultisampleCount < MultisampleCount)
            {
                // マルチサンプリング数が少ない場合、レンダ ターゲットもこれに合わせる。
                MultisampleCount = depthStencilMultisampleCount;
                MultisampleQuality = depthStencilMultisampleQuality;
            }
            else if (depthStencilMultisampleCount == MultisampleCount &&
                depthStencilMultisampleQuality < MultisampleQuality)
            {
                // マルチサンプリング数が同じでも、
                // マルチサンプリング品質レベルが低い場合、レンダ ターゲットもこれに合わせる。
                MultisampleQuality = depthStencilMultisampleQuality;
            }

            // レンダ ターゲットのマルチサンプリング性能の方が低い場合は、
            // 深度ステンシルもこれに合わせる。

            InitializeRenderTarget();

            if (DepthStencilEnabled)
                DepthStencil = InitializeDepthStencil();
        }

        protected sealed override void InitializeCore(Stream stream)
        {
            InitializeRenderTarget(stream);

            if (DepthStencilEnabled)
                DepthStencil = InitializeDepthStencil();
        }

        protected abstract void InitializeRenderTarget(SwapChain swapChain, int index);

        protected abstract void InitializeRenderTarget();

        protected abstract void InitializeRenderTarget(Stream stream);

        protected abstract DepthStencil InitializeDepthStencil();

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                if (renderTargetView != null)
                    renderTargetView.Dispose();

                if (DepthStencil != null)
                    DepthStencil.Dispose();
            }

            base.DisposeOverride(disposing);
        }
    }
}
