#region Using

using System;
using System.IO;
using System.Runtime.InteropServices;

using D3D11BindFlags = SharpDX.Direct3D11.BindFlags;
using D3D11CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags;
using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11ImageFileFormat = SharpDX.Direct3D11.ImageFileFormat;
using D3D11MapFlags = SharpDX.Direct3D11.MapFlags;
using D3D11MapMode = SharpDX.Direct3D11.MapMode;
using D3D11Resource = SharpDX.Direct3D11.Resource;
using D3D11ResourceOptionFlags = SharpDX.Direct3D11.ResourceOptionFlags;
using D3D11ResourceRegion = SharpDX.Direct3D11.ResourceRegion;
using D3D11ResourceUsage = SharpDX.Direct3D11.ResourceUsage;
using D3D11Texture2D = SharpDX.Direct3D11.Texture2D;
using D3D11Texture2DDescription = SharpDX.Direct3D11.Texture2DDescription;
using DXGIFormat = SharpDX.DXGI.Format;
using SDXUtilities = SharpDX.Utilities;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxRenderTarget : RenderTarget
    {
        public D3D11Device D3D11Device { get; private set; }

        public D3D11Texture2D D3D11Texture2D { get; private set; }

        public SdxRenderTarget(SdxDevice device, bool isBackBuffer)
            : base(device, isBackBuffer)
        {
            D3D11Device = device.D3D11Device;
        }

        protected override void InitializeCore(SwapChain swapChain, int index)
        {
            var dxgiSwapChain = (swapChain as SdxSwapChain).DXGISwapChain;

            D3D11Texture2D = D3D11Texture2D.FromSwapChain<D3D11Texture2D>(dxgiSwapChain, index);

            var description = D3D11Texture2D.Description;

            if (!FormatHelper.DXGIFormatsAsSurfaceFormat.Contains(description.Format))
                throw new InvalidOperationException("Format not supported: " + description.Format);

            Width = description.Width;
            Height = description.Height;
            MipLevels = description.MipLevels;
            Format = (SurfaceFormat) description.Format;
            MultisampleCount = description.SampleDescription.Count;
            MultisampleQuality = description.SampleDescription.Quality;
            Usage = (ResourceUsage) description.Usage;

            if (DepthFormat != DepthFormat.None)
                DepthStencil = InitializeDepthStencil();
        }

        protected override void InitializeRenderTarget()
        {
            if (Usage != ResourceUsage.Default) throw new ArgumentException("ResourceUsage.Default required.", "usage");
            if (Width < 1) throw new InvalidOperationException("Width < 1: " + Width);
            if (Height < 1) throw new InvalidOperationException("Height < 1: " + Height);
            if (MipLevels < 0) throw new InvalidOperationException("MipLevels < 0: " + MipLevels);
            if (MultisampleCount < 1) throw new InvalidOperationException("MultisampleCount < 1: " + MultisampleCount);
            if (MultisampleQuality < 0) throw new InvalidOperationException("MultisampleQuality < 0: " + MultisampleQuality);

            D3D11Texture2DDescription description;
            CreateD3D11Texture2DDescription(out description);

            D3D11Texture2D = new D3D11Texture2D(D3D11Device, description);
        }

        protected override void InitializeRenderTarget(Stream stream)
        {
            if (Usage != ResourceUsage.Default) throw new ArgumentException("ResourceUsage.Default required.", "usage");

            D3D11Texture2D = D3D11Resource.FromStream<D3D11Texture2D>(D3D11Device, stream, (int) stream.Length);

            var description = D3D11Texture2D.Description;

            if (!FormatHelper.DXGIFormatsAsSurfaceFormat.Contains(description.Format))
                throw new InvalidOperationException("Format not supported: " + description.Format);

            Width = description.Width;
            Height = description.Height;
            MipLevels = description.MipLevels;
            Format = (SurfaceFormat) description.Format;
            MultisampleCount = description.SampleDescription.Count;
            MultisampleQuality = description.SampleDescription.Quality;
            Usage = (ResourceUsage) description.Usage;
        }

        protected override DepthStencil InitializeDepthStencil()
        {
            var depthStencil = new SdxDepthStencil(Device as SdxDevice)
            {
                Width = Width,
                Height = Height,
                Format = DepthFormat,
                MultisampleCount = MultisampleCount,
                MultisampleQuality = MultisampleQuality
            };

            depthStencil.Initialize();

            return depthStencil;
        }

        protected override void SaveCore(DeviceContext context, Stream stream, ImageFileFormat format = ImageFileFormat.Png)
        {
            var d3d11DeviceContext = (context as SdxDeviceContext).D3D11DeviceContext;

            D3D11Resource.ToStream(d3d11DeviceContext, D3D11Texture2D, (D3D11ImageFileFormat) format, stream);
        }

        protected override void GetDataCore<T>(
            DeviceContext context, int level, Rectangle? rectangle, T[] data, int startIndex, int elementCount)
        {
            // TODO
            //
            // SdxTexture2D と同じコード。
            // 共通化したいが、継承関係が無いため困難。

            int w;
            int h;

            if (rectangle.HasValue)
            {
                // 矩形が設定されているならば、これにサイズを合わせる。
                w = rectangle.Value.Width;
                h = rectangle.Value.Height;
            }
            else
            {
                // ミップマップのサイズ。
                w = Width >> level;
                h = Height >> level;
            }

            var description = new D3D11Texture2DDescription
            {
                Width = w,
                Height = h,
                MipLevels = 1,
                ArraySize = 1,
                Format = (DXGIFormat) Format,
                SampleDescription =
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = D3D11ResourceUsage.Staging,
                BindFlags = D3D11BindFlags.None,
                CpuAccessFlags = D3D11CpuAccessFlags.Read,
                OptionFlags = D3D11ResourceOptionFlags.None
            };

            D3D11ResourceRegion? d3d11ResourceRegion = null;
            if (rectangle.HasValue)
            {
                d3d11ResourceRegion = new D3D11ResourceRegion
                {
                    Left = rectangle.Value.Left,
                    Top = rectangle.Value.Top,
                    Right = rectangle.Value.Right,
                    Bottom = rectangle.Value.Bottom
                };
            }

            var d3dDeviceContext = (context as SdxDeviceContext).D3D11DeviceContext;
            using (var staging = new D3D11Texture2D(D3D11Device, description))
            {
                d3dDeviceContext.CopySubresourceRegion(D3D11Texture2D, level, d3d11ResourceRegion, staging, 1);

                var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    var dataPointer = gcHandle.AddrOfPinnedObject();
                    var sizeOfT = Marshal.SizeOf(typeof(T));
                    var destinationPointer = (IntPtr) (dataPointer + startIndex * sizeOfT);
                    var sizeInBytes = ((elementCount == 0) ? data.Length : elementCount) * sizeOfT;

                    var mappedResource = d3dDeviceContext.MapSubresource(staging, level, D3D11MapMode.Read, D3D11MapFlags.None);
                    try
                    {
                        SDXUtilities.CopyMemory(destinationPointer, mappedResource.DataPointer, sizeInBytes);
                    }
                    finally
                    {
                        d3dDeviceContext.UnmapSubresource(staging, level);
                    }
                }
                finally
                {
                    gcHandle.Free();
                }
            }
        }

        void CreateD3D11Texture2DDescription(out D3D11Texture2DDescription result)
        {
            result = new D3D11Texture2DDescription
            {
                Width = Width,
                Height = Height,
                MipLevels = MipLevels,
                ArraySize = 1,
                Format = (DXGIFormat) Format,
                SampleDescription =
                {
                    Count = MultisampleCount,
                    Quality = MultisampleQuality
                },
                Usage = (D3D11ResourceUsage) Usage,
                BindFlags = D3D11BindFlags.ShaderResource | D3D11BindFlags.RenderTarget,
                CpuAccessFlags = ResourceHelper.GetD3D11CpuAccessFlags((D3D11ResourceUsage) Usage),
                OptionFlags = (MipLevels != 1) ? D3D11ResourceOptionFlags.GenerateMipMaps : D3D11ResourceOptionFlags.None
            };
        }

        #region IDisposable

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                if (D3D11Texture2D != null)
                    D3D11Texture2D.Dispose();
            }

            base.DisposeOverride(disposing);
        }

        #endregion
    }
}
