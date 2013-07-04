#region Using

using System;
using System.Runtime.InteropServices;

using D3D11BindFlags = SharpDX.Direct3D11.BindFlags;
using D3D11Buffer = SharpDX.Direct3D11.Buffer;
using D3D11BufferDescription = SharpDX.Direct3D11.BufferDescription;
using D3D11CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags;
using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11ResourceOptionFlags = SharpDX.Direct3D11.ResourceOptionFlags;
using D3D11ResourceUsage = SharpDX.Direct3D11.ResourceUsage;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxIndexBuffer : IndexBuffer
    {
        public D3D11Device D3D11Device { get; private set; }

        public D3D11Buffer D3D11Buffer { get; private set; }

        public SdxIndexBuffer(SdxDevice device)
            : base(device)
        {
            D3D11Device = device.D3D11Device;
        }

        protected override void InitializeCore()
        {
            D3D11BufferDescription description;
            CreateD3D11BufferDescription(out description);

            D3D11Buffer = new D3D11Buffer(D3D11Device, description);
        }

        protected override void InitializeCore<T>(T[] data)
        {
            D3D11BufferDescription description;
            CreateD3D11BufferDescription(out description);

            D3D11Buffer = D3D11Buffer.Create<T>(D3D11Device, data, description);
        }

        void CreateD3D11BufferDescription(out D3D11BufferDescription result)
        {
            result = new D3D11BufferDescription
            {
                SizeInBytes = ByteWidth,
                Usage = (D3D11ResourceUsage) Usage,
                BindFlags = D3D11BindFlags.IndexBuffer,
                CpuAccessFlags = ResourceHelper.GetD3D11CpuAccessFlags((D3D11ResourceUsage) Usage),
                OptionFlags = D3D11ResourceOptionFlags.None,
                StructureByteStride = 0
            };
        }

        #region IDisposable

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                if (D3D11Buffer != null)
                    D3D11Buffer.Dispose();
            }

            base.DisposeOverride(disposing);
        }

        #endregion
    }
}
