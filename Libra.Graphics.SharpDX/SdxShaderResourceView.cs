#region Using

using System;

using D3DShaderResourceViewDimension = SharpDX.Direct3D.ShaderResourceViewDimension;
using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11Resource = SharpDX.Direct3D11.Resource;
using D3D11ShaderResourceView = SharpDX.Direct3D11.ShaderResourceView;
using D3D11ShaderResourceViewDescription = SharpDX.Direct3D11.ShaderResourceViewDescription;
using DXGIFormat = SharpDX.DXGI.Format;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxShaderResourceView : ShaderResourceView
    {
        public D3D11Device D3D11Device { get; private set; }

        public D3D11ShaderResourceView D3D11ShaderResourceView { get; private set; }

        public SdxShaderResourceView(SdxDevice device)
            : base(device)
        {
            D3D11Device = device.D3D11Device;
        }

        protected override void InitializeCore()
        {
            if (Resource is SdxTexture2D)
            {
                var d3d11Texture2D = (Resource as SdxTexture2D).D3D11Texture2D;
                D3D11ShaderResourceView = new D3D11ShaderResourceView(D3D11Device, d3d11Texture2D);
            }
            else if (Resource is SdxRenderTarget)
            {
                var d3d11Texture2D = (Resource as SdxRenderTarget).D3D11Texture2D;
                D3D11ShaderResourceView = new D3D11ShaderResourceView(D3D11Device, d3d11Texture2D);
            }
            else if (Resource is SdxDepthStencil)
            {
                var d3d11Texture2D = (Resource as SdxDepthStencil).D3D11Texture2D;

                if (1 < d3d11Texture2D.Description.ArraySize)
                    throw new InvalidOperationException("The depth stencil does not support the array of textures.");

                var description = new D3D11ShaderResourceViewDescription
                {
                    Format = (DXGIFormat) ResolveShaderResourceViewForDepth(d3d11Texture2D.Description.Format),
                };

                // テクスチャ配列ではない事を基底クラスの実装で保証する前提。

                if (d3d11Texture2D.Description.SampleDescription.Quality == 0)
                {
                    // 非マルチサンプリング テクスチャ。
                    description.Texture2D.MipLevels = d3d11Texture2D.Description.MipLevels;
                    description.Texture2D.MostDetailedMip = 0;
                    description.Dimension = D3DShaderResourceViewDimension.Texture2D;
                }
                else
                {
                    // マルチサンプリング テクスチャ。
                    description.Dimension = D3DShaderResourceViewDimension.Texture2DMultisampled;
                }

                D3D11ShaderResourceView = new D3D11ShaderResourceView(D3D11Device, d3d11Texture2D, description);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        // 深度ステンシルのテクスチャ フォーマットは、
        // 深度ステンシル ビューのフォーマットに対応する Typless 系フォーマットである。
        // シェーダ リソース ビューでは、これを更に変換して設定する必要がある。

        DXGIFormat ResolveShaderResourceViewForDepth(DXGIFormat depthStencilTextureFormat)
        {
            switch (depthStencilTextureFormat)
            {
                case DXGIFormat.R32_Float_X8X24_Typeless:
                    return DXGIFormat.R32_Float_X8X24_Typeless;
                case DXGIFormat.R32_Typeless:
                    return DXGIFormat.R32_Float;
                case DXGIFormat.R24G8_Typeless:
                    return DXGIFormat.R24_UNorm_X8_Typeless;
                case DXGIFormat.R16_Typeless:
                    return DXGIFormat.R16_UNorm;
                default:
                    throw new InvalidOperationException(string.Format("Format '{0}' is not supported.", depthStencilTextureFormat));
            }
        }

        #region IDisposable

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                if (D3D11ShaderResourceView != null)
                    D3D11ShaderResourceView.Dispose();
            }

            base.DisposeOverride(disposing);
        }

        #endregion
    }
}
