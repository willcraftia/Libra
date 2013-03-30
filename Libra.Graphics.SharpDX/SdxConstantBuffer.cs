#region Using

using System;
using System.Runtime.InteropServices;

using D3D11BindFlags = SharpDX.Direct3D11.BindFlags;
using D3D11Buffer = SharpDX.Direct3D11.Buffer;
using D3D11BufferDescription = SharpDX.Direct3D11.BufferDescription;
using D3D11CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags;
using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11MapFlags = SharpDX.Direct3D11.MapFlags;
using D3D11MapMode = SharpDX.Direct3D11.MapMode;
using D3D11ResourceOptionFlags = SharpDX.Direct3D11.ResourceOptionFlags;
using D3D11ResourceUsage = SharpDX.Direct3D11.ResourceUsage;
using SDXUtilities = SharpDX.Utilities;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxConstantBuffer : ConstantBuffer
    {
        public int ByteWidth { get; private set; }

        public D3D11Device D3D11Device { get; private set; }

        public D3D11Buffer D3D11Buffer { get; private set; }

        public SdxConstantBuffer(SdxDevice device)
            : base(device)
        {
            D3D11Device = device.D3D11Device;
        }

        protected override void InitializeCore(int byteWidth)
        {
            ByteWidth = byteWidth;

            D3D11BufferDescription description;
            CreateD3D11BufferDescription(out description);

            D3D11Buffer = new D3D11Buffer(D3D11Device, description);
        }

        protected override void InitializeCore<T>(int byteWidth, T data)
        {
            ByteWidth = byteWidth;

            D3D11BufferDescription description;
            CreateD3D11BufferDescription(out description);

            D3D11Buffer = D3D11Buffer.Create<T>(D3D11Device, ref data, description);
        }

        protected override void GetDataCore<T>(DeviceContext context, out T data)
        {
            var stagingDescription = new D3D11BufferDescription
            {
                SizeInBytes = ByteWidth,
                Usage = D3D11ResourceUsage.Staging,
                BindFlags = D3D11BindFlags.None,
                CpuAccessFlags = D3D11CpuAccessFlags.Read,
                OptionFlags = D3D11ResourceOptionFlags.None,
                StructureByteStride = 0
            };

            var d3dDeviceContext = (context as SdxDeviceContext).D3D11DeviceContext;
            using (var staging = new D3D11Buffer(D3D11Device, stagingDescription))
            {
                d3dDeviceContext.CopyResource(D3D11Buffer, staging);

                var mappedResource = d3dDeviceContext.MapSubresource(staging, 0, D3D11MapMode.Read, D3D11MapFlags.None);
                try
                {
                    // data はスタックに作る事になるので、CopyMemory ではない。
                    data = (T) Marshal.PtrToStructure(mappedResource.DataPointer, typeof(T));
                }
                finally
                {
                    d3dDeviceContext.UnmapSubresource(staging, 0);
                }
            }
        }

        void CreateD3D11BufferDescription(out D3D11BufferDescription result)
        {
            result = new D3D11BufferDescription
            {
                SizeInBytes = ByteWidth,
                Usage = (D3D11ResourceUsage) Usage,
                BindFlags = D3D11BindFlags.ConstantBuffer,
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
