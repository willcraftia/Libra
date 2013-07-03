#region Using

using System;

using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11RenderTargetView = SharpDX.Direct3D11.RenderTargetView;
using D3D11RenderTargetViewDescription = SharpDX.Direct3D11.RenderTargetViewDescription;
using D3D11RenderTargetViewDimension = SharpDX.Direct3D11.RenderTargetViewDimension;
using D3D11ShaderResourceView = SharpDX.Direct3D11.ShaderResourceView;
using DXGIFormat = SharpDX.DXGI.Format;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxRenderTargetView : RenderTargetView
    {
        public D3D11Device D3D11Device { get; private set; }

        public D3D11RenderTargetView D3D11RenderTargetView { get; private set; }

        public SdxRenderTargetView(SdxDevice device)
            : base(device)
        {
            D3D11Device = device.D3D11Device;
        }

        protected override void InitializeRenderTargetView()
        {
            D3D11RenderTargetViewDescription description;
            CreateD3D11RenderTargetViewDescription(out description);

            var d3d11Resource = (RenderTarget as SdxRenderTarget).D3D11Texture2D;

            D3D11RenderTargetView = new D3D11RenderTargetView(D3D11Device, d3d11Resource, description);
        }

        void CreateD3D11RenderTargetViewDescription(out D3D11RenderTargetViewDescription result)
        {
            result = new D3D11RenderTargetViewDescription
            {
                Format = (DXGIFormat) RenderTarget.Format,
                Texture2D =
                {
                    MipSlice = 0
                }
            };

            if (1 < RenderTarget.MultisampleCount)
            {
                result.Dimension = D3D11RenderTargetViewDimension.Texture2DMultisampled;
            }
            else
            {
                result.Dimension = D3D11RenderTargetViewDimension.Texture2D;
            }
        }

        #region IDisposable

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                if (D3D11RenderTargetView != null)
                    D3D11RenderTargetView.Dispose();
            }

            base.DisposeOverride(disposing);
        }

        #endregion
    }
}
