#region Using

using System;
using System.Collections.Generic;
using Libra.Graphics;

#endregion

namespace Libra.Games
{
    public class GraphicsManager : IGraphicsManager, IGraphicsService, IDisposable
    {
        // メモ
        //
        // DeviceLost:
        //      D3D11 ではデバイス ロストが無いので不要。
        //
        // DeviceResetting/DeviceReset:
        //      デバイスのリセットも考える必要がないのでは？
        //      あり得るのはアダプタあるいはプロファイルの変更だが。
        //      自分には重要ではないので DeviceResetting/DeviceReset イベントは不要。
        //      アダプタやプロファイルを変更したいなら、アプリケーション全体を再初期化すれば良い。
        //
        // PreparingDeviceSettings:
        //      GrahicsDeviceInformation から SwapChain 設定を除外し、アダプタと表示モードに特化させたため、
        //      生成されたインスタンスをイベントで受信して修正することはもうできない。
        //      2D 用の深度ステンシル バッファ無効化ならば、PreferredDepthStencilFormat で None を指定すれば良い。
        //      よって、PreparingDeviceSettings イベントは不要。
        //
        // FindBestDevice/RankDevices
        //      アダプタ選択とモニタの表示モード選択を分離。
        //      および、それらをオーバライド可能なメソッドで定義。
        //

        public event EventHandler DeviceCreated;

        public event EventHandler DeviceDisposing;
        
        public event EventHandler Disposed;

        public const int DefaultBackBufferWidth = 800;

        public const int DefaultBackBufferHeight = 480;

        public const SurfaceFormat DefaultBackBufferFormat = SurfaceFormat.Color;

        public const DepthFormat DefaultDepthStencilFormat = DepthFormat.Depth24Stencil8;

        // 自動的に利用可能なサンプル数が選択されるので、
        // デフォルトは最大サンプル数とする。
        public const int DefaultBackBufferMultisampleCount = D3D11Constants.MaxMultisampleCount;

        static readonly DeviceProfile[] profiles =
        {
            DeviceProfile.Level_11_0,
            DeviceProfile.Level_10_1,
            DeviceProfile.Level_10_0,
        };

        Game game;

        GraphicsFactory graphicsFactory;

        int preferredBackBufferWidth;

        int preferredBackBufferHeight;

        SurfaceFormat preferredBackBufferFormat;

        DepthFormat preferredDepthStencilFormat;

        int preferredBackBufferMultisampleCount;

        object resizeSwapChainLock = new object();

        public bool DeviceSingleThreaded { get; set; }

        public bool DeviceDebugEnabled { get; set; }

        public Device Device { get; private set; }

        public SwapChain SwapChain { get; private set; }

        public int PreferredBackBufferWidth
        {
            get { return preferredBackBufferWidth; }
            set
            {
                if (preferredBackBufferWidth <= 0) throw new ArgumentOutOfRangeException("value");
                preferredBackBufferWidth = value;
            }
        }

        public int PreferredBackBufferHeight
        {
            get { return preferredBackBufferHeight; }
            set
            {
                if (preferredBackBufferHeight <= 0) throw new ArgumentOutOfRangeException("value");
                preferredBackBufferHeight = value;
            }
        }

        public SurfaceFormat PreferredBackBufferFormat
        {
            get { return preferredBackBufferFormat; }
            set
            {
                // 32 ビットを越えるフォーマットを拒否。
                if (value != SurfaceFormat.Bgr565 &&
                    value != SurfaceFormat.Bgra5551 &&
                    value != SurfaceFormat.Color &&
                    value != SurfaceFormat.Rgba1010102)
                    throw new ArgumentOutOfRangeException("value");

                preferredBackBufferFormat = value;
            }
        }

        public DepthFormat PreferredDepthStencilFormat
        {
            get { return preferredDepthStencilFormat; }
            set { preferredDepthStencilFormat = value; }
        }

        public bool PreferMultisampling { get; set; }

        public int PreferredBackBufferMultisampleCount
        {
            get { return preferredBackBufferMultisampleCount; }
            set
            {
                if (!MathHelper.IsPowerOf2(value))
                    throw new ArgumentException("Value must be a power of 2.", "value");

                preferredBackBufferMultisampleCount = value;
            }
        }

        public bool SynchronizeWithVerticalRetrace { get; set; }

        public bool Windowed { get; set; }

        public GraphicsManager(Game game)
        {
            if (game == null) throw new ArgumentNullException("game");
            if (game.Services.GetService(typeof(IGraphicsService)) != null)
                throw new ArgumentException("GraphicsDeviceManager is already registered.", "game");

            this.game = game;

            DeviceSingleThreaded = true;
            DeviceDebugEnabled = false;

            preferredBackBufferWidth = DefaultBackBufferWidth;
            preferredBackBufferHeight = DefaultBackBufferHeight;
            preferredBackBufferFormat = DefaultBackBufferFormat;
            preferredDepthStencilFormat = DefaultDepthStencilFormat;
            preferredBackBufferMultisampleCount = DefaultBackBufferMultisampleCount;
            Windowed = true;

            game.Services.AddService<IGraphicsManager>(this);
            game.Services.AddService<IGraphicsService>(this);
        }

        public void Initialize()
        {
            // ファクトリの取得。
            graphicsFactory = GraphicsFactory.CreateGraphicsFactory();

            InitializeDevice();
            InitializeSwapChain();

            Device.SetSwapChain(SwapChain);
        }

        void InitializeDevice()
        {
            var settings = new DeviceSettings
            {
                SingleThreaded = DeviceSingleThreaded,
                Debug = DeviceDebugEnabled
            };

            var adapter = ResolveAdapter();

            Device = graphicsFactory.CreateDevice(adapter, settings, profiles);
            Device.Disposing += OnDeviceDisposing;

            OnDeviceCreated(this, EventArgs.Empty);
        }

        void InitializeSwapChain()
        {
            DisplayMode backBufferMode;
            if (Windowed)
            {
                var monitorMode = ResolveOutputMode(Device);

                backBufferMode = new DisplayMode
                {
                    Width = preferredBackBufferWidth,
                    Height = preferredBackBufferHeight,
                    Format = preferredBackBufferFormat,
                    // 現在の設定と思われるモニタのリフレッシュ レートに従う。
                    RefreshRate = monitorMode.RefreshRate
                };
            }
            else
            {
                backBufferMode = ResolveFullScreenBackBufferMode(Device);
            }

            // マルチサンプル数の決定。
            int backBufferMultisampleCount = preferredBackBufferMultisampleCount;
            int backBufferMultisampleQualityLevels = 0;
            while (1 < backBufferMultisampleCount)
            {
                backBufferMultisampleQualityLevels = Device.CheckMultisampleQualityLevels(
                    backBufferMode.Format, backBufferMultisampleCount);

                if (0 < backBufferMultisampleQualityLevels)
                {
                    // 深度ステンシルを用いる場合、
                    // 深度ステンシルのマルチサンプリングはレンダ ターゲットと一致させる事になるため、
                    // 深度ステンシルのフォーマットについても検査。
                    if (preferredDepthStencilFormat != DepthFormat.None)
                    {
                        var depthStencilMultisampleQualityLevels = Device.CheckMultisampleQualityLevels(
                            preferredDepthStencilFormat, backBufferMultisampleCount);

                        if (0 < depthStencilMultisampleQualityLevels)
                        {
                            // 0 より大きいならば有効なサンプル数。
                            // 最終的な品質レベルは低い方に合わせる。
                            backBufferMultisampleQualityLevels = Math.Min(
                                backBufferMultisampleQualityLevels, depthStencilMultisampleQualityLevels);
                            break;
                        }
                    }
                    else
                    {
                        // 0 より大きいならば有効なサンプル数。
                        break;
                    }
                }

                // 指定のフォーマットでは無効なサンプル数であったため、
                // サンプル数を一段階落として再検査を試行。
                backBufferMultisampleCount /= 2;
            }

            var settings = new SwapChainSettings
            {
                BackBufferWidth = backBufferMode.Width,
                BackBufferHeight = backBufferMode.Height,
                BackBufferRefreshRate = backBufferMode.RefreshRate,
                BackBufferFormat = backBufferMode.Format,
                BackBufferMultisampleCount = backBufferMultisampleCount,
                // 注意: Quality = 最大レベル - 1
                BackBufferMultisampleQuality = backBufferMultisampleQualityLevels - 1,
                DepthStencilFormat = preferredDepthStencilFormat,
                // TODO
                // false にしても [alt]+[Enter] が有効なのだが？
                AllowModeSwitch = true,
                SyncInterval = (SynchronizeWithVerticalRetrace) ? 1 : 0,
                Windowed = Windowed,
                OutputWindow = game.Window.Handle
            };

            // スワップ チェーンの初期化。
            SwapChain = graphicsFactory.CreateSwapChain(Device, settings);

            // バック バッファのサイズへウィンドウをリサイズ。
            SwapChain.ResizeTarget();
        }

        protected virtual Adapter ResolveAdapter()
        {
            // デフォルト実装ではデフォルト アダプタを利用。
            // 推奨設定に見合う表示モードをデフォルト アダプタが提供できるか否かを考慮しない。

            return graphicsFactory.DefaultAdapter;
        }

        protected virtual DisplayMode ResolveOutputMode(Device device)
        {
            // デフォルト実装では、デフォルト アダプタの主モニタを対象に、
            // デスクトップへの出力に相応しいモードを検索。

            var output = graphicsFactory.DefaultAdapter.PrimaryOutput;

            // 主モニタよりデスクトップ サイズを取得。
            var desktopBounds = output.DesktopCoordinates;

            // デスクトップと同サイズ、かつ、SurfaceFormat.Color が条件。
            var preferredMode = new DisplayMode
            {
                Width = desktopBounds.Width,
                Height = desktopBounds.Height,
                Format = SurfaceFormat.Color
            };

            // デスクトップ出力に用いられている表示モードを検索。
            DisplayMode resolvedMode;
            output.GetClosestMatchingMode(device, ref preferredMode, out resolvedMode);

            return resolvedMode;
        }

        protected virtual DisplayMode ResolveFullScreenBackBufferMode(Device device)
        {
            // デフォルト実装では、推奨設定に最も適合する表示モードを、
            // DXGI の機能により主モニタの表示モードから自動選択。

            // リフレッシュ レート未指定で探索。
            var preferredMode = new DisplayMode
            {
                Width = preferredBackBufferWidth,
                Height = preferredBackBufferHeight,
                Format = preferredBackBufferFormat
            };

            DisplayMode resolvedMode;
            device.Adapter.PrimaryOutput.GetClosestMatchingMode(device, ref preferredMode, out resolvedMode);

            return resolvedMode;
        }

        public bool BeginDraw()
        {
            if (Device == null)
                return false;

            return true;
        }

        public void EndDraw()
        {
            SwapChain.Present();
        }

        public void ToggleFullScreen()
        {
            // TODO
        }

        protected virtual void OnDeviceCreated(Object sender, EventArgs e)
        {
            if (DeviceCreated != null)
                DeviceCreated(this, e);
        }

        protected virtual void OnDeviceDisposing(object sender, EventArgs e)
        {
            if (DeviceDisposing != null)
                DeviceDisposing(this, e);
        }

        #region IDisposable

        bool disposed;

        ~GraphicsManager()
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
                if (SwapChain != null)
                    SwapChain.Dispose();

                if (Device != null)
                    Device.Dispose();
            }

            disposed = true;

            if (Disposed != null)
                Disposed(this, EventArgs.Empty);
        }

        #endregion
    }
}
