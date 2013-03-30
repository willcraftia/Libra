#region Using

using System;

#endregion

namespace Libra.Graphics
{
    // SwapChainSettings は、D3D9 のPrensetParameters や XNA の PresentationParameters に相当。
    // それらは、D3D11 では SwapChain と明確に対応する DXGI_SWAP_CHAIN_DESC であり、
    // ここでは SwapChainSettings として DXGI_SWAP_CHAIN_DESC を表現している。

    public struct SwapChainSettings
    {
        /// <summary>
        /// バック バッファの幅。
        /// </summary>
        /// <remarks>
        /// DXGI_SWAP_CHAIN_DESC.BufferDesc.Width。
        /// </remarks>
        public int BackBufferWidth;

        /// <summary>
        /// バック バッファの高さ。
        /// </summary>
        /// <remarks>
        /// DXGI_SWAP_CHAIN_DESC.BufferDesc.Height。
        /// </remarks>
        public int BackBufferHeight;

        /// <summary>
        /// バック バッファの反映に用いるリフレッシュ レート。
        /// </summary>
        /// <remarks>
        /// DXGI_SWAP_CHAIN_DESC.BufferDesc.RefreshRate。
        /// </remarks>
        public RefreshRate BackBufferRefreshRate;

        /// <summary>
        /// バック バッファのフォーマット。
        /// </summary>
        /// <remarks>
        /// DXGI_SWAP_CHAIN_DESC.BufferDesc.Format。
        /// </remarks>
        public SurfaceFormat BackBufferFormat;

        /// <summary>
        /// バック バッファのマルチサンプリング数。
        /// </summary>
        /// <remarks>
        /// DXGI_SWAP_CHAIN_DESC.SampleDesc.Count。
        /// </remarks>
        public int BackBufferMultisampleCount;

        /// <summary>
        /// バック バッファのマルチサンプリング品質。
        /// </summary>
        /// <remarks>
        /// DXGI_SWAP_CHAIN_DESC.SampleDesc.Quality。
        /// </remarks>
        public int BackBufferMultisampleQuality;
        
        /// <summary>
        /// 出力ウィンドウのハンドル。
        /// </summary>
        /// <remarks>
        /// DXGI_SWAP_CHAIN_DESC.OutputWindow。
        /// </remarks>
        public IntPtr OutputWindow;
        
        /// <summary>
        /// ウィンドウ表示か否かを示す値。
        /// </summary>
        /// <remarks>
        /// DXGI_SWAP_CHAIN_DESC.Windowed。
        /// </remarks>
        /// <value>
        /// true (ウィンドウ表示)、false (全画面表示)。
        /// </value>
        public bool Windowed;

        /// <summary>
        /// 表示モードを切り替えられるか否かを示す値。
        /// </summary>
        /// <remarks>
        /// DXGI_SWAP_CHAIN_DESC.Flags の DXGI_SWAP_CHAIN_FLAG.DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH。
        /// </remarks>
        /// <value>
        /// true (表示モードを切り替えられる場合)、false (それ以外の場合)。
        /// </value>
        public bool AllowModeSwitch;

        /// <summary>
        /// 深度ステンシルのフォーマット。
        /// </summary>
        /// <remarks>
        /// このプロパティは、深度ステンシル バッファの作成で利用されます。
        /// DepthFormat.None を指定した場合、
        /// スワップ チェーンは深度ステンシル バッファを作成しません。
        /// </remarks>
        public DepthFormat DepthStencilFormat;

        /// <summary>
        /// 描画間隔。
        /// </summary>
        /// <remarks>
        /// この値は、IDXGISwapChain::Present の SyncInterval 引数として利用されます。
        /// </remarks>
        public int SyncInterval;
    }
}
