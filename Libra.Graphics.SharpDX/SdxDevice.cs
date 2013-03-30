#region Using

using System;
using System.Collections.Generic;

using D3D11Blend = SharpDX.Direct3D11.BlendOption;
using D3D11BlendOperation = SharpDX.Direct3D11.BlendOperation;
using D3D11BlendState = SharpDX.Direct3D11.BlendState;
using D3D11BlendStateDescription = SharpDX.Direct3D11.BlendStateDescription;
using D3D11ColorWriteMaskFlags = SharpDX.Direct3D11.ColorWriteMaskFlags;
using D3D11Comparison = SharpDX.Direct3D11.Comparison;
using D3D11CullMode = SharpDX.Direct3D11.CullMode;
using D3D11DepthStencilState = SharpDX.Direct3D11.DepthStencilState;
using D3D11DepthStencilStateDescription = SharpDX.Direct3D11.DepthStencilStateDescription;
using D3D11DepthWriteMask = SharpDX.Direct3D11.DepthWriteMask;
using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11DeviceChild = SharpDX.Direct3D11.DeviceChild;
using D3D11DeviceContext = SharpDX.Direct3D11.DeviceContext;
using D3D11DeviceCreationFlags = SharpDX.Direct3D11.DeviceCreationFlags;
using D3D11Feature = SharpDX.Direct3D11.Feature;
using D3D11FillMode = SharpDX.Direct3D11.FillMode;
using D3D11Filter = SharpDX.Direct3D11.Filter;
using D3D11RasterizerState = SharpDX.Direct3D11.RasterizerState;
using D3D11RasterizerStateDescription = SharpDX.Direct3D11.RasterizerStateDescription;
using D3D11SamplerState = SharpDX.Direct3D11.SamplerState;
using D3D11SamplerStateDescription = SharpDX.Direct3D11.SamplerStateDescription;
using D3D11StencilOperation = SharpDX.Direct3D11.StencilOperation;
using D3D11Texture2D = SharpDX.Direct3D11.Texture2D;
using D3D11TextureAddressMode = SharpDX.Direct3D11.TextureAddressMode;
using D3DDriverType = SharpDX.Direct3D.DriverType;
using D3DFeatureLevel = SharpDX.Direct3D.FeatureLevel;
using DXGIFormat = SharpDX.DXGI.Format;
using SDXSharpDXException = SharpDX.SharpDXException;

#endregion

namespace Libra.Graphics.SharpDX
{
    using BlendStateManager = SdxStateManager<BlendState, D3D11BlendState>;
    using SamplerStateManager = SdxStateManager<SamplerState, D3D11SamplerState>;
    using RasterizerStateManager = SdxStateManager<RasterizerState, D3D11RasterizerState>;
    using DepthStencilStateManager = SdxStateManager<DepthStencilState, D3D11DepthStencilState>;

    public sealed class SdxDevice : IDevice
    {
        // D3D11 ではデバイス ロストが無いので DeviceLost イベントは不要。

        // デバイスのリセットも考える必要がないのでは？
        // あり得るとしたらアダプタあるいはプロファイルの変更だが・・・そんな事する？
        // 自分には重要ではないので DeviceResetting/DeviceReset イベントは不要。
        // アダプタやプロファイルを変更したいなら、アプリケーション全体を再初期化すれば良い。

        // ResourceCreated/ResourceDestroyed イベントは実装しない。
        // ShaprDX では、各リソースに対応するクラスのコンストラクタで
        // Device のリソース生成メソッドを隠蔽しているため、
        // イベントを発生させるためのトリガーを作れない。

        public event EventHandler Disposing;

        public event EventHandler BackBuffersResetting;

        public event EventHandler BackBuffersReset;

        public readonly DeviceSettings Settings;

        RenderTarget backBufferRenderTarget;

        public IAdapter Adapter { get; private set; }

        public DeviceProfile Profile { get; private set; }

        public SdxDeviceFeatures Features { get; private set; }

        public DeviceContext ImmediateContext { get; private set; }

        public D3D11Device D3D11Device { get; private set; }

        public BlendStateManager BlendStateManager { get; private set; }

        public SamplerStateManager SamplerStateManager { get; private set; }

        public RasterizerStateManager RasterizerStateManager { get; private set; }

        public DepthStencilStateManager DepthStencilStateManager { get; private set; }

        public RenderTargetView BackBufferRenderTargetView { get; private set; }

        public SdxDevice(SdxAdapter adapter, DeviceSettings settings, DeviceProfile[] profiles)
        {
            Adapter = adapter;
            Settings = settings;

            // デバイスの初期化。
            var creationFlags = ResolveD3D11DeviceCreationFlags(settings);
            var featureLevels = ToD3DFeatureLevels(profiles);
            if (Adapter != null)
            {
                try
                {
                    var dxgiAdapter = (Adapter as SdxAdapter).DXGIAdapter;
                    D3D11Device = new D3D11Device(dxgiAdapter, creationFlags, featureLevels);
                }
                catch (SDXSharpDXException)
                {
                    try
                    {
                        D3D11Device = new D3D11Device(D3DDriverType.Warp, creationFlags, featureLevels);
                    }
                    catch (SDXSharpDXException)
                    {
                        D3D11Device = new D3D11Device(D3DDriverType.Reference, creationFlags, featureLevels);
                    }
                }
            }
            else
            {
                try
                {
                    D3D11Device = new D3D11Device(D3DDriverType.Warp, creationFlags, featureLevels);
                }
                catch (SDXSharpDXException)
                {
                    D3D11Device = new D3D11Device(D3DDriverType.Reference, creationFlags, featureLevels);
                }
            }

            // コンテキストの初期化。
            ImmediateContext = new SdxDeviceContext(this, D3D11Device.ImmediateContext);

            // デバイス生成で選択されたプロファイルの記録。
            Profile = (DeviceProfile) D3D11Device.FeatureLevel;

            // デバイス特性の記録。
            Features = SdxDeviceFeatures.Create(D3D11Device);

            // ブレンド ステートの初期化。
            BlendStateManager = new BlendStateManager(this, CreateD3D11BlendState);
            BlendStateManager.Register(BlendState.Additive);
            BlendStateManager.Register(BlendState.AlphaBlend);
            BlendStateManager.Register(BlendState.NonPremultiplied);
            BlendStateManager.Register(BlendState.Opaque);

            // サンプラ ステートの初期化。
            SamplerStateManager = new SamplerStateManager(this, CreateD3D11SamplerState);
            SamplerStateManager.Register(SamplerState.AnisotropicClamp);
            SamplerStateManager.Register(SamplerState.AnisotropicWrap);
            SamplerStateManager.Register(SamplerState.LinearClamp);
            SamplerStateManager.Register(SamplerState.LinearWrap);
            SamplerStateManager.Register(SamplerState.PointClamp);
            SamplerStateManager.Register(SamplerState.PointWrap);

            // レンダ ステートの初期化。
            RasterizerStateManager = new RasterizerStateManager(this, CreateD3D11RasterizerState);
            RasterizerStateManager.Register(RasterizerState.CullNone);
            RasterizerStateManager.Register(RasterizerState.CullFront);
            RasterizerStateManager.Register(RasterizerState.CullBack);

            // 深度ステンシル ステートの初期化。
            DepthStencilStateManager = new DepthStencilStateManager(this, CreateD3D11DepthStencilState);
            DepthStencilStateManager.Register(DepthStencilState.Default);
            DepthStencilStateManager.Register(DepthStencilState.DepthRead);
            DepthStencilStateManager.Register(DepthStencilState.None);
        }

        public DeviceContext CreateDeferredContext()
        {
            var d3d11DeviceContext = new D3D11DeviceContext(D3D11Device);
            return new SdxDeviceContext(this, d3d11DeviceContext);
        }

        public VertexShader CreateVertexShader()
        {
            return new SdxVertexShader(this);
        }

        public PixelShader CreatePixelShader()
        {
            return new SdxPixelShader(this);
        }

        public InputLayout CreateInputLayout()
        {
            return new SdxInputLayout(this);
        }

        public ConstantBuffer CreateConstantBuffer()
        {
            return new SdxConstantBuffer(this);
        }

        public VertexBuffer CreateVertexBuffer()
        {
            return new SdxVertexBuffer(this);
        }

        public IndexBuffer CreateIndexBuffer()
        {
            return new SdxIndexBuffer(this);
        }

        public Texture2D CreateTexture2D()
        {
            return new SdxTexture2D(this);
        }

        public DepthStencil CreateDepthStencil()
        {
            return new SdxDepthStencil(this);
        }

        public RenderTarget CreateRenderTarget()
        {
            return new SdxRenderTarget(this, false);
        }

        public ShaderResourceView CreateShaderResourceView()
        {
            return new SdxShaderResourceView(this);
        }

        public DepthStencilView CreateDepthStencilView()
        {
            return new SdxDepthStencilView(this);
        }

        public RenderTargetView CreateRenderTargetView()
        {
            return new SdxRenderTargetView(this);
        }

        public OcclusionQuery CreateOcclusionQuery()
        {
            return new SdxOcclusionQuery(this);
        }

        public int CheckMultisampleQualityLevels(SurfaceFormat format, int sampleCount)
        {
            if (sampleCount < 1) throw new ArgumentOutOfRangeException("sampleCount");

            if (sampleCount == 1) return 0;

            return D3D11Device.CheckMultisampleQualityLevels((DXGIFormat) format, sampleCount);
        }

        public int CheckMultisampleQualityLevels(DepthFormat format, int sampleCount)
        {
            if (sampleCount < 1) throw new ArgumentOutOfRangeException("sampleCount");

            if (sampleCount == 1) return 0;

            return D3D11Device.CheckMultisampleQualityLevels((DXGIFormat) format, sampleCount);
        }

        public void SetSwapChain(SwapChain swapChain)
        {
            swapChain.BackBuffersResizing += OnSwapChainBackBuffersResizing;
            swapChain.BackBuffersResized += OnSwapChainBackBuffersResized;

            InitializeBackBufferRenderTarget(swapChain);

            // バック バッファ レンダ ターゲットの設定。
            ImmediateContext.SetRenderTarget(null);
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

            backBufferRenderTarget = CreateRenderTarget();
            backBufferRenderTarget.Name = "BackBuffer_0";
            backBufferRenderTarget.DepthFormat = swapChain.DepthStencilFormat;
            backBufferRenderTarget.RenderTargetUsage = RenderTargetUsage.Preserve;
            backBufferRenderTarget.Initialize(swapChain, 0);

            BackBufferRenderTargetView = CreateRenderTargetView();
            BackBufferRenderTargetView.Initialize(backBufferRenderTarget);

            if (BackBuffersReset != null)
                BackBuffersReset(this, EventArgs.Empty);
        }

        void ReleaseBackBufferRenderTarget()
        {
            if (BackBuffersResetting != null)
                BackBuffersResetting(this, EventArgs.Empty);

            if (backBufferRenderTarget != null)
            {
                backBufferRenderTarget.Dispose();
                backBufferRenderTarget = null;
            }
            if (BackBufferRenderTargetView != null)
            {
                BackBufferRenderTargetView.Dispose();
                BackBufferRenderTargetView = null;
            }
        }

        D3DFeatureLevel[] ToD3DFeatureLevels(DeviceProfile[] profiles)
        {
            if (profiles == null || profiles.Length == 0)
                return null;

            var result = new D3DFeatureLevel[profiles.Length];
            for (int i = 0; i < profiles.Length; i++)
                result[i] = (D3DFeatureLevel) profiles[i];

            return result;
        }

        D3D11BlendState CreateD3D11BlendState(BlendState state)
        {
            var description = D3D11BlendStateDescription.Default();

            description.AlphaToCoverageEnable = false;
            description.IndependentBlendEnable = false;

            bool isBlendEnabled = true;
            if (state.ColorSourceBlend == Blend.One &&
                state.AlphaSourceBlend == Blend.One &&
                state.ColorDestinationBlend == Blend.Zero &&
                state.AlphaDestinationBlend == Blend.Zero)
                isBlendEnabled = false;

            description.RenderTarget[0].IsBlendEnabled = isBlendEnabled;
            description.RenderTarget[0].SourceBlend = (D3D11Blend) state.ColorSourceBlend;
            description.RenderTarget[0].DestinationBlend = (D3D11Blend) state.ColorDestinationBlend;
            description.RenderTarget[0].BlendOperation = (D3D11BlendOperation) state.ColorBlendFunction;
            description.RenderTarget[0].SourceAlphaBlend = (D3D11Blend) state.AlphaSourceBlend;
            description.RenderTarget[0].DestinationAlphaBlend = (D3D11Blend) state.AlphaDestinationBlend;
            description.RenderTarget[0].AlphaBlendOperation = (D3D11BlendOperation) state.AlphaBlendFunction;
            description.RenderTarget[0].RenderTargetWriteMask = (D3D11ColorWriteMaskFlags) state.ColorWriteChannels;

            // TODO
            // ColorWriteChannels1-3 は RenderTarget[1]-RenderTarget[3] か？

            return new D3D11BlendState(D3D11Device, description);
        }

        D3D11SamplerState CreateD3D11SamplerState(SamplerState state)
        {
            var description = D3D11SamplerStateDescription.Default();

            description.AddressU = (D3D11TextureAddressMode) state.AddressU;
            description.AddressV = (D3D11TextureAddressMode) state.AddressV;
            description.AddressW = (D3D11TextureAddressMode) state.AddressW;
            description.Filter = (D3D11Filter) state.Filter;
            description.MaximumAnisotropy = state.MaxAnisotropy;
            description.MipLodBias = state.MipMapLodBias;
            description.ComparisonFunction = (D3D11Comparison) state.ComparisonFunction;
            description.BorderColor = state.BorderColor.ToSDXColor4();
            description.MinimumLod = state.MinLod;
            description.MaximumLod = state.MaxLod;

            return new D3D11SamplerState(D3D11Device, description);
        }

        D3D11RasterizerState CreateD3D11RasterizerState(RasterizerState state)
        {
            var description = new D3D11RasterizerStateDescription
            {
                FillMode = (D3D11FillMode) state.FillMode,
                CullMode = (D3D11CullMode) state.CullMode,
                IsFrontCounterClockwise = false,
                DepthBias = state.DepthBias,
                SlopeScaledDepthBias = state.SlopeScaleDepthBias,
                DepthBiasClamp = state.DepthBiasClamp,
                IsDepthClipEnabled = state.DepthClipEnable,
                IsScissorEnabled = state.ScissorEnable,
                IsMultisampleEnabled = state.MultisampleEnable,
                IsAntialiasedLineEnabled = state.AntialiasedLineEnable
            };

            return new D3D11RasterizerState(D3D11Device, description);
        }

        D3D11DepthStencilState CreateD3D11DepthStencilState(DepthStencilState state)
        {
            var description = new D3D11DepthStencilStateDescription
            {
                IsDepthEnabled = state.DepthEnable,
                DepthWriteMask = (state.DepthWriteEnable) ? D3D11DepthWriteMask.All : D3D11DepthWriteMask.Zero,
                DepthComparison = (D3D11Comparison) state.DepthFunction,
                IsStencilEnabled = state.StencilEnable,
                StencilReadMask = state.StencilReadMask,
                StencilWriteMask = state.StencilWriteMask,
                FrontFace =
                {
                    FailOperation = (D3D11StencilOperation) state.FrontFaceStencilFail,
                    DepthFailOperation = (D3D11StencilOperation) state.FrontFaceStencilDepthFail,
                    PassOperation = (D3D11StencilOperation) state.FrontFaceStencilPass,
                    Comparison = (D3D11Comparison) state.FrontFaceStencilFunction
                },
                BackFace =
                {
                    FailOperation = (D3D11StencilOperation) state.BackFaceStencilFail,
                    DepthFailOperation = (D3D11StencilOperation) state.BackFaceStencilDepthFail,
                    PassOperation = (D3D11StencilOperation) state.BackFaceStencilPass,
                    Comparison = (D3D11Comparison) state.BackFaceStencilFunction
                }
            };

            return new D3D11DepthStencilState(D3D11Device, description);
        }

        D3D11DeviceCreationFlags ResolveD3D11DeviceCreationFlags(DeviceSettings settings)
        {
            // TODO
            // 後で再考。

            var result = D3D11DeviceCreationFlags.None;

            if (settings.SingleThreaded)
                result |= D3D11DeviceCreationFlags.SingleThreaded;

            if (settings.Debug)
                result |= D3D11DeviceCreationFlags.Debug;

            return result;
        }

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~SdxDevice()
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
            if (IsDisposed) return;

            if (Disposing != null)
                Disposing(this, EventArgs.Empty);

            if (disposing)
            {
                BlendStateManager.Dispose();
                SamplerStateManager.Dispose();
                RasterizerStateManager.Dispose();
                DepthStencilStateManager.Dispose();
                ImmediateContext.Dispose();
                D3D11Device.Dispose();
            }

            IsDisposed = true;
        }

        #endregion
    }
}
