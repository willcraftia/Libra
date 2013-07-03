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

        Dictionary<Type, WeakReference> sharedResourceMap;

        RenderTarget backBuffer;

        public Adapter Adapter { get; private set; }

        public DeviceSettings Settings { get; private set; }

        public abstract DeviceProfile Profile { get; }

        public abstract DeviceContext ImmediateContext { get; }

        public int BackBufferWidth
        {
            get { return backBuffer.Width; }
        }

        public int BackBufferHeight
        {
            get { return backBuffer.Height; }
        }

        public int BackBufferMipLevels
        {
            get { return backBuffer.MipLevels; }
        }

        public SurfaceFormat BackBufferFormat
        {
            get { return backBuffer.Format; }
        }

        public int BackBufferMultisampleCount
        {
            get { return backBuffer.MultisampleCount; }
        }

        public int BackBufferMultisampleQuality
        {
            get { return backBuffer.MultisampleQuality; }
        }

        public bool BackBufferDepthStencilEnabled
        {
            get { return backBuffer.DepthStencilEnabled; }
        }

        public SurfaceFormat BackBufferDepthStencilFormat
        {
            get { return backBuffer.DepthStencilFormat; }
        }

        internal RenderTargetView BackBufferView { get; private set; }

        protected Device(Adapter adapter, DeviceSettings settings)
        {
            Adapter = adapter;
            Settings = settings;

            sharedResourceMap = new Dictionary<Type, WeakReference>();
        }

        public TSharedResource GetSharedResource<TKey, TSharedResource>() where TSharedResource : class
        {
            return GetSharedResource(typeof(TKey), typeof(TSharedResource)) as TSharedResource;
        }

        /// <summary>
        /// デバイス単位で共有するリソースを共有リソース キャッシュより取得します。
        /// 共有リソース キャッシュに指定のリソースが存在しない場合には、
        /// 新たに生成するインスタンスをキャッシュに追加してから返却します。
        /// 
        /// キャッシュする共有リソースのクラスは、
        /// Device を引数とする公開コンストラクタを定義しなければなりません。
        /// 
        /// 共有リソースの利用側クラスは、このメソッドにより取得した共有リソースへの参照を
        /// インスタンス フィールドで保持する必要があります。
        /// これは、デバイスは共有リソースを弱参照でキャッシュしているためです。
        /// 
        /// 共有リソースを弱参照で管理しているため、
        /// どのインスタンスからも参照されなくなった共有リソースはデバイスから自動的に削除されます。
        /// </summary>
        /// <remarks>
        /// 共有リソースは、主に一連のグラフィックス パイプライン処理を纏めたクラスで利用します。
        /// そのようなクラスで利用する頂点シェーダやピクセル シェーダはデバイスに一つだけ存在すれば良く、
        /// また、デバイス コンテキスト単位で動的に状態を変更する事がないため、共有リソースの対象となります。
        /// 一方、デバイス コンテキストで動的な変更を伴うリソースは、共有リソースの対象ではありません。
        /// 例えば、Usage が Immutable の定数バッファは共有リソースの候補ですが、
        /// Default や Dynamic の定数バッファは共有リソースとすべきではありません。
        /// </remarks>
        /// <param name="key"></param>
        /// <param name="sharedResourceType"></param>
        /// <returns></returns>
        public object GetSharedResource(Type key, Type sharedResourceType)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (sharedResourceType == null) throw new ArgumentNullException("sharedResourceType");

            lock (sharedResourceMap)
            {
                object sharedResource = null;
                WeakReference reference;
                if (sharedResourceMap.TryGetValue(key, out reference))
                {
                    sharedResource = reference.Target;
                }
                else
                {
                    reference = new WeakReference(null);
                    sharedResourceMap[key] = reference;
                }

                if (sharedResource == null)
                {
                    sharedResource = Activator.CreateInstance(sharedResourceType, this);
                    reference.Target = sharedResource;
                }

                return sharedResource;
            }
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

            backBuffer = CreateRenderTarget();
            backBuffer.Name = "BackBuffer_0";
            backBuffer.DepthStencilEnabled = swapChain.DepthStencilEnabled;
            backBuffer.DepthStencilFormat = swapChain.DepthStencilFormat;
            backBuffer.RenderTargetUsage = RenderTargetUsage.Preserve;
            backBuffer.Initialize(swapChain, 0);

            BackBufferView = CreateRenderTargetView();
            BackBufferView.Initialize(backBuffer);

            OnBackBuffersReset(this, EventArgs.Empty);
        }

        void ReleaseBackBufferRenderTarget()
        {
            OnBackBuffersResetting(this, EventArgs.Empty);

            if (backBuffer != null)
            {
                backBuffer.Dispose();
                backBuffer = null;
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
