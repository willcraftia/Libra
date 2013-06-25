#region Using

using System;
using System.IO;

#endregion

namespace Libra.Graphics
{
    public abstract class RenderTarget : Texture2D
    {
        internal event EventHandler BindingToOutputMerger;

        DepthFormat depthFormat;

        RenderTargetView renderTargetView;

        public bool IsBackBuffer { get; private set; }

        public DepthFormat DepthFormat
        {
            get { return depthFormat; }
            set
            {
                AssertNotInitialized();

                depthFormat = value;
            }
        }

        public RenderTargetUsage RenderTargetUsage { get; set; }

        public DepthStencil DepthStencil { get; private set; }

        protected RenderTarget(Device device, bool isBackBuffer)
            : base(device)
        {
            IsBackBuffer = isBackBuffer;

            depthFormat = DepthFormat.None;
            RenderTargetUsage = RenderTargetUsage.Discard;
        }

        // バック バッファ用初期化メソッド。
        public void Initialize(SwapChain swapChain, int index)
        {
            AssertNotInitialized();
            if (swapChain == null) throw new ArgumentNullException("swapChain");
            if (index < 0) throw new ArgumentOutOfRangeException("index");

            InitializeRenderTarget(swapChain, index);

            if (depthFormat != DepthFormat.None)
                DepthStencil = InitializeDepthStencil();

            initialized = true;
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
            // TODO
            // そもそも、そのサンプル数が無効 (multisampleQualityLevels = 0) の場合はどうするのだ？

            var multisampleQualityLevels = Device.CheckMultisampleQualityLevels(depthFormat, MultisampleCount);
            if (0 < multisampleQualityLevels)
            {
                // テクスチャと深度ステンシルで低い方の品質へ合わせる。
                MultisampleQuality = Math.Min(MultisampleQuality, multisampleQualityLevels - 1);
            }
            else
            {
                // TODO
                // ひとまず無効化。
                MultisampleQuality = 0;
            }

            InitializeRenderTarget();

            if (depthFormat != DepthFormat.None)
                DepthStencil = InitializeDepthStencil();
        }

        protected sealed override void InitializeCore(Stream stream)
        {
            InitializeRenderTarget(stream);

            if (depthFormat != DepthFormat.None)
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

        internal void OnBindingToOutputMerger()
        {
            if (BindingToOutputMerger != null)
                BindingToOutputMerger(this, EventArgs.Empty);
        }
    }
}
