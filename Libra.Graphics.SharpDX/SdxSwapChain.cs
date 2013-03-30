#region Using

using System;

// DXGI インタフェースのみで構成したい。
using DXGIFactory1 = SharpDX.DXGI.Factory1;
using DXGIFormat = SharpDX.DXGI.Format;
using DXGIModeDescription = SharpDX.DXGI.ModeDescription;
using DXGIPresentFlags = SharpDX.DXGI.PresentFlags;
using DXGISwapChain = SharpDX.DXGI.SwapChain;
using DXGISwapChainDescription = SharpDX.DXGI.SwapChainDescription;
using DXGISwapChainFlags = SharpDX.DXGI.SwapChainFlags;
using DXGISwapEffect = SharpDX.DXGI.SwapEffect;
using DXGIUsage = SharpDX.DXGI.Usage;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxSwapChain : SwapChain
    {
        /// <summary>
        /// バック バッファ数。
        /// </summary>
        public const int BackBufferCount = 2;

        int backBufferWidth;

        int backBufferHeight;
        
        SurfaceFormat backBufferFormat;
        
        int backBufferMultiSampleCount;
        
        int backBufferMultiSampleQuality;
        
        bool windowed;
        
        bool allowModeSwitch;
        
        DepthFormat depthStencilFormat;
        
        int syncInterval;

        RefreshRate refreshRate;

        DXGISwapChainFlags dxgiSwapChainFlags;

        public override int BackBufferWidth
        {
            get { return backBufferWidth; }
        }

        public override int BackBufferHeight
        {
            get { return backBufferHeight; }
        }

        public override SurfaceFormat BackBufferFormat
        {
            get { return backBufferFormat; }
        }

        public override int BackBufferMultiSampleCount
        {
            get { return backBufferMultiSampleCount; }
        }

        public override int BackBufferMultiSampleQuality
        {
            get { return backBufferMultiSampleQuality; }
        }

        public override bool Windowed
        {
            get { return windowed; }
        }

        public override bool AllowModeSwitch
        {
            get { return allowModeSwitch; }
        }

        public override DepthFormat DepthStencilFormat
        {
            get { return depthStencilFormat; }
        }

        public override int SyncInterval
        {
            get { return syncInterval; }
        }

        public DXGISwapChain DXGISwapChain { get; private set; }

        public SdxSwapChain(SdxDevice device, SwapChainSettings settings)
            : base(device)
        {
            depthStencilFormat = settings.DepthStencilFormat;
            allowModeSwitch = settings.AllowModeSwitch;
            syncInterval = settings.SyncInterval;

            InitializeDXGISwapChain(ref settings);
        }

        public override void Present()
        {
            // TODO
            // DXGIPresentFlags を理解できていない。
            DXGISwapChain.Present(SyncInterval, DXGIPresentFlags.None);
        }

        protected override void ResizeBuffersCore(int width, int height, int bufferCount, SurfaceFormat format)
        {
            // ResizeBuffers
            //
            // バック バッファのサイズ、フォーマット、バッファ数を変更。
            // ウィンドウのサイズが変更された時に呼び出す必要がある。

            backBufferWidth = width;
            backBufferHeight = height;
            backBufferFormat = format;

            DXGISwapChain.ResizeBuffers(bufferCount, width, height, (DXGIFormat) format, dxgiSwapChainFlags);
        }

        protected override void ResizeTargetCore(int width, int height)
        {
            // ResizeTarget
            //
            // ・ウィンドウ モードの場合は対象ウィンドウのサイズを変更
            // ・全画面モードの場合は表示モードを変更
            //
            // アプリケーションは、このメソッド呼び出しで対象ウィンドウのサイズを変更できます、とのこと。

            var description = new DXGIModeDescription
            {
                Width = width,
                Height = height,
                // リフレッシュ レート設定を維持。
                RefreshRate =
                {
                    Numerator = refreshRate.Numerator,
                    Denominator = refreshRate.Denominator
                },
                // フォーマット設定を維持。
                Format = (DXGIFormat) BackBufferFormat
            };

            DXGISwapChain.ResizeTarget(ref description);

        }

        void InitializeDXGISwapChain(ref SwapChainSettings settings)
        {
            dxgiSwapChainFlags = DXGISwapChainFlags.None;
            // DXGISwapChainFlags.AllowModeSwitch のみ対応。
            if (AllowModeSwitch)
                dxgiSwapChainFlags |= DXGISwapChainFlags.AllowModeSwitch;

            var description = new DXGISwapChainDescription
            {
                ModeDescription =
                {
                    Width = settings.BackBufferWidth,
                    Height = settings.BackBufferHeight,
                    RefreshRate =
                    {
                        Numerator = settings.BackBufferRefreshRate.Numerator,
                        Denominator = settings.BackBufferRefreshRate.Denominator
                    },
                    Format = (DXGIFormat) settings.BackBufferFormat
                },
                SampleDescription =
                {
                    Count = settings.BackBufferMultisampleCount,
                    Quality = settings.BackBufferMultisampleQuality
                },
                Usage = DXGIUsage.RenderTargetOutput | DXGIUsage.ShaderInput,
                BufferCount = BackBufferCount,
                OutputHandle = settings.OutputWindow,
                IsWindowed = settings.Windowed,
                SwapEffect = DXGISwapEffect.Discard,
                Flags = dxgiSwapChainFlags
            };

            var sdxDevice = Device as SdxDevice;
            var dxgiFactory = (sdxDevice.Adapter as SdxAdapter).DXGIAdapter.GetParent<DXGIFactory1>();
            DXGISwapChain = new DXGISwapChain(dxgiFactory, sdxDevice.D3D11Device, description);

            // スワップ チェーンの生成では、推測により値が決定されるものも含まれるため、
            // スワップ チェーンの設定を示すプロパティについては、
            // スワップ チェーンの生成後に保持する。

            description = DXGISwapChain.Description;

            backBufferWidth = description.ModeDescription.Width;
            backBufferHeight = description.ModeDescription.Height;
            backBufferFormat = (SurfaceFormat) description.ModeDescription.Format;
            refreshRate = new RefreshRate
            {
                Numerator = description.ModeDescription.RefreshRate.Numerator,
                Denominator = description.ModeDescription.RefreshRate.Denominator
            };
            backBufferMultiSampleCount = description.SampleDescription.Count;
            backBufferMultiSampleQuality = description.SampleDescription.Quality;
            windowed = description.IsWindowed;

            dxgiSwapChainFlags = description.Flags;
        }

        #region IDisposable

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                DXGISwapChain.Dispose();
            }

            base.DisposeOverride(disposing);
        }

        #endregion
    }
}
