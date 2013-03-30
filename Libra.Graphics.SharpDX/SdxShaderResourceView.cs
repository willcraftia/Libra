#region Using

using System;

using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11Resource = SharpDX.Direct3D11.Resource;
using D3D11ShaderResourceView = SharpDX.Direct3D11.ShaderResourceView;

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
            D3D11Resource d3d11Resource = null;
            if (Resource is SdxTexture2D)
            {
                d3d11Resource = (Resource as SdxTexture2D).D3D11Texture2D;
            }
            else
            {
                throw new NotSupportedException();
            }

            D3D11ShaderResourceView = new D3D11ShaderResourceView(D3D11Device, d3d11Resource);
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
