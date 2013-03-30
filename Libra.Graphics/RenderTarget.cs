#region Using

using System;
using System.IO;

#endregion

namespace Libra.Graphics
{
    public abstract class RenderTarget : Texture2D
    {
        bool initialized;

        public bool IsBackBuffer { get; private set; }

        public DepthFormat DepthFormat { get; set; }

        public RenderTargetUsage RenderTargetUsage { get; set; }

        public DepthStencil DepthStencil { get; protected set; }

        protected RenderTarget(IDevice device, bool isBackBuffer)
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

            InitializeCore(swapChain, index);

            initialized = true;
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

        protected abstract void InitializeCore(SwapChain swapChain, int index);

        protected abstract void InitializeRenderTarget();

        protected abstract void InitializeRenderTarget(Stream stream);

        protected abstract DepthStencil InitializeDepthStencil();

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                if (DepthStencil != null)
                    DepthStencil.Dispose();
            }

            base.DisposeOverride(disposing);
        }
    }
}
