#region Using

using System;

using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11VertexShader = SharpDX.Direct3D11.VertexShader;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxVertexShader : VertexShader
    {
        public D3D11Device D3D11Device { get; private set; }

        public D3D11VertexShader D3D11VertexShader { get; private set; }

        public SdxVertexShader(SdxDevice device)
            : base(device)
        {
            D3D11Device = device.D3D11Device;
        }

        protected override void InitializeCore()
        {
            D3D11VertexShader = new D3D11VertexShader(D3D11Device, ShaderBytecode);
        }

        #region IDisposable

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                if (D3D11VertexShader != null)
                    D3D11VertexShader.Dispose();
            }

            base.DisposeOverride(disposing);
        }

        #endregion
    }
}
