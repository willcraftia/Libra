#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics
{
    public abstract class Device : IDisposable
    {
        // D3D11 ではデバイス ロストが無いので DeviceLost イベントは不要。

        // デバイスのリセットも考える必要がないのでは？
        // あり得るとしたらアダプタあるいはプロファイル？そんな事する？
        // 自分には重要ではないので DeviceResetting/DeviceReset イベントは不要。
        // アダプタやプロファイルを変更したいなら、アプリケーション全体を再初期化。

        // ResourceCreated/ResourceDestroyed イベントは実装しない。
        // ShaprDX では、各リソースに対応するクラスのコンストラクタで
        // Device のリソース生成メソッドを隠蔽しているため、
        // イベントを発生させるためのトリガーを作れない。

        public event EventHandler Disposing;

        public event EventHandler BackBuffersResetting;

        public event EventHandler BackBuffersReset;

        public IAdapter Adapter { get; private set; }

        public DeviceSettings Settings { get; private set; }

        public abstract DeviceProfile Profile { get; }

        public abstract DeviceContext ImmediateContext { get; }

        public RenderTarget BackBuffer { get; private set; }

        public RenderTargetView BackBufferView { get; private set; }

        protected Device(IAdapter adapter, DeviceSettings settings)
        {
            Adapter = adapter;
            Settings = settings;
        }

        public abstract DeviceContext CreateDeferredContext();

        public abstract VertexShader CreateVertexShader();

        public abstract PixelShader CreatePixelShader();

        public abstract InputLayout CreateInputLayout();

        public abstract ConstantBuffer CreateConstantBuffer();

        public abstract VertexBuffer CreateVertexBuffer();

        public abstract IndexBuffer CreateIndexBuffer();

        public abstract Texture2D CreateTexture2D();

        public abstract DepthStencil CreateDepthStencil();

        public abstract RenderTarget CreateRenderTarget();

        public abstract ShaderResourceView CreateShaderResourceView();

        public abstract DepthStencilView CreateDepthStencilView();

        public abstract RenderTargetView CreateRenderTargetView();

        public abstract OcclusionQuery CreateOcclusionQuery();

        public abstract int CheckMultisampleQualityLevels(SurfaceFormat format, int sampleCount);

        public abstract int CheckMultisampleQualityLevels(DepthFormat format, int sampleCount);

        public void SetSwapChain(SwapChain swapChain)
        {
            swapChain.BackBuffersResizing += OnSwapChainBackBuffersResizing;
            swapChain.BackBuffersResized += OnSwapChainBackBuffersResized;

            InitializeBackBufferRenderTarget(swapChain);

            // バック バッファ レンダ ターゲットの設定。
            ImmediateContext.SetRenderTarget(null);
        }

        protected virtual void OnBackBuffersResetting(object sender, EventArgs e)
        {
            if (BackBuffersResetting != null)
                BackBuffersResetting(sender, e);
        }

        protected virtual void OnBackBuffersReset(object sender, EventArgs e)
        {
            if (BackBuffersReset != null)
                BackBuffersReset(sender, e);
        }


        void OnSwapChainBackBuffersResizing(object sender, EventArgs e)
        {
            ReleaseBackBufferRenderTarget();
        }

        void OnSwapChainBackBuffersResized(object sender, EventArgs e)
        {
            InitializeBackBufferRenderTarget(sender as SwapChain);
        }

        void InitializeBackBufferRenderTarget(SwapChain swapChain)
        {
            // バッファ リサイズ時にバッファの破棄が発生するため、
            // 深度ステンシルを共有している設定は自由に破棄できずに都合が悪い。
            // よって、共有不可 (RenderTargetUsage.Preserve) でレンダ ターゲットを生成。

            BackBuffer = CreateRenderTarget();
            BackBuffer.Name = "BackBuffer_0";
            BackBuffer.DepthFormat = swapChain.DepthStencilFormat;
            BackBuffer.RenderTargetUsage = RenderTargetUsage.Preserve;
            BackBuffer.Initialize(swapChain, 0);

            BackBufferView = CreateRenderTargetView();
            BackBufferView.Initialize(BackBuffer);

            OnBackBuffersReset(this, EventArgs.Empty);
        }

        void ReleaseBackBufferRenderTarget()
        {
            OnBackBuffersResetting(this, EventArgs.Empty);

            if (BackBuffer != null)
            {
                BackBuffer.Dispose();
                BackBuffer = null;
            }
            if (BackBufferView != null)
            {
                BackBufferView.Dispose();
                BackBufferView = null;
            }
        }

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~Device()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeOverride(bool disposing) { }

        void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (Disposing != null)
                Disposing(this, EventArgs.Empty);

            DisposeOverride(disposing);

            if (disposing)
            {
                ImmediateContext.Dispose();
            }

            IsDisposed = true;
        }

        #endregion
    }
}
