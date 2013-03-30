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
    public sealed class SdxIndexBuffer : IndexBuffer
    {
        public int ByteWidth { get; private set; }

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
            ByteWidth = Marshal.SizeOf(typeof(T)) * data.Length;

            D3D11BufferDescription description;
            CreateD3D11BufferDescription(out description);

            D3D11Buffer = D3D11Buffer.Create<T>(D3D11Device, data, description);
        }

        protected override void GetDataCore<T>(DeviceContext context, T[] data, int startIndex, int elementCount)
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

                var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    var dataPointer = gcHandle.AddrOfPinnedObject();
                    var sizeOfT = Marshal.SizeOf(typeof(T));
                    var destinationPtr = (IntPtr) (dataPointer + startIndex * sizeOfT);
                    var sizeInBytes = ((elementCount == 0) ? data.Length : elementCount) * sizeOfT;

                    var mappedResource = d3dDeviceContext.MapSubresource(staging, 0, D3D11MapMode.Read, D3D11MapFlags.None);
                    try
                    {
                        SDXUtilities.CopyMemory(destinationPtr, mappedResource.DataPointer, sizeInBytes);
                    }
                    finally
                    {
                        d3dDeviceContext.UnmapSubresource(staging, 0);
                    }
                }
                finally
                {
                    gcHandle.Free();
                }
            }
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
