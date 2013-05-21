#region Using

using System;
using System.IO;

#endregion

namespace Libra.Graphics
{
    public abstract class RenderTarget : Texture2D
    {
        internal event EventHandler BindingToOutputMerger;

        bool initialized;

        RenderTargetView renderTargetView;

        public bool IsBackBuffer { get; private set; }

        public DepthFormat DepthFormat { get; set; }

        public RenderTargetUsage RenderTargetUsage { get; set; }

        public DepthStencil DepthStencil { get; private set; }

        protected RenderTarget(Device device, bool isBackBuffer)
            : base(device)
        {
            IsBackBuffer = isBackBuffer;

            DepthFormat = DepthFormat.None;
            RenderTargetUsage = RenderTargetUsage.Discard;
        }

        // バック バッファ用初期化メソッド。
        public void Initialize(SwapChain swapChain, int index)
        {
            if (initialized) throw new InvalidOperationException("Already initialized.");
            if (swapChain == null) throw new ArgumentNullException("swapChain");
            if (index < 0) throw new ArgumentOutOfRangeException("index");

            InitializeRenderTarget(swapChain, index);

            if (DepthFormat != DepthFormat.None)
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
            InitializeRenderTarget();

            if (DepthFormat != DepthFormat.None)
                DepthStencil = InitializeDepthStencil();
        }

        protected sealed override void InitializeCore(Stream stream)
        {
            InitializeRenderTarget(stream);

            if (DepthFormat != DepthFormat.None)
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
