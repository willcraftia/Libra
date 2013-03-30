#region Using

using System;

using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11DepthStencilView = SharpDX.Direct3D11.DepthStencilView;
using D3D11DepthStencilViewDescription = SharpDX.Direct3D11.DepthStencilViewDescription;
using D3D11DepthStencilViewDimension = SharpDX.Direct3D11.DepthStencilViewDimension;
using D3D11DepthStencilViewFlags = SharpDX.Direct3D11.DepthStencilViewFlags;
using DXGIFormat = SharpDX.DXGI.Format;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxDepthStencilView : DepthStencilView
    {
        public D3D11Device D3D11Device { get; private set; }

        public D3D11DepthStencilView D3D11DepthStencilView { get; private set; }

        public SdxDepthStencilView(SdxDevice device)
            : base(device)
        {
            D3D11Device = device.D3D11Device;
        }

        protected override void InitializeCore()
        {
            D3D11DepthStencilViewDescription description;
            CreateD3D11DepthStencilViewDescription(out description);

            var d3d11Resource = (DepthStencil as SdxDepthStencil).D3D11Texture2D;

            D3D11DepthStencilView = new D3D11DepthStencilView(D3D11Device, d3d11Resource, description);
        }

        void CreateD3D11DepthStencilViewDescription(out D3D11DepthStencilViewDescription result)
        {
            result = new D3D11DepthStencilViewDescription
            {
                Format = (DXGIFormat) DepthStencil.Format,
                Flags = D3D11DepthStencilViewFlags.None,
                Texture2D =
                {
                    MipSlice = 0
                }
            };

            if (1 < DepthStencil.MultisampleCount)
            {
                result.Dimension = D3D11DepthStencilViewDimension.Texture2DMultisampled;
            }
            else
            {
                result.Dimension = D3D11DepthStencilViewDimension.Texture2D;
            }
        }

        #region IDisposable

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                if (D3D11DepthStencilView != null)
                    D3D11DepthStencilView.Dispose();
            }

            base.DisposeOverride(disposing);
        }

        #endregion
    }
}
