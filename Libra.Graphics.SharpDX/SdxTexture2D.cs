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
    public class SdxTexture2D : Texture2D
    {
        // TODO
        //
        // 以下のメモは何か勘違いしている。
        // D3DX11_IMAGE_LOAD_INFO に画像形式指定などない。
        //
        // メモ
        //
        // ・ストリームからの生成では ResourceUsage 決定を D3D11Resource.FromStream に委任
        //      これを指定可能とするには D3DX11_IMAGE_LOAD_INFO の全指定などが必要となり、
        //      画像データの自動認識が行えなくなる。
        //      ここでは、自動認識の利点を優先し、ResourceUsage 指定を破棄する。

        public D3D11Device D3D11Device { get; private set; }

        public D3D11Texture2D D3D11Texture2D { get; private set; }

        public SdxTexture2D(SdxDevice device)
            : base(device)
        {
            D3D11Device = device.D3D11Device;
        }

        protected override void InitializeCore()
        {
            D3D11Texture2DDescription description;
            CreateD3D11Texture2DDescription(out description);

            // メモ
            //
            // パラメータの組み合わせにより D3D11 側でインスタンス化できるか否かが変化する。
            // 例えば、Usage = Default と MipLevels = 11 で生成できたとしても、
            // Usage = Dynamic と MipLevels = 11 では生成に失敗するなど。

            D3D11Texture2D = new D3D11Texture2D(D3D11Device, description);
        }

        protected override void InitializeCore(Stream stream)
        {
            // TODO
            //
            // ImageLoadInformation で明示しないと Usage が Default 固定になってしまう。

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

        protected override void SaveCore(DeviceContext context, Stream stream, ImageFileFormat format = ImageFileFormat.Png)
        {
            var d3d11DeviceContext = (context as SdxDeviceContext).D3D11DeviceContext;

            D3D11Resource.ToStream(d3d11DeviceContext, D3D11Texture2D, (D3D11ImageFileFormat) format, stream);
        }

        protected override void GetDataCore<T>(
            DeviceContext context, int level, Rectangle? rectangle, T[] data, int startIndex, int elementCount)
        {
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

            var stagingDescription = new D3D11Texture2DDescription
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
            using (var staging = new D3D11Texture2D(D3D11Device, stagingDescription))
            {
                d3dDeviceContext.CopySubresourceRegion(D3D11Texture2D, level, d3d11ResourceRegion, staging, 0);
                
                var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    var dataPointer = gcHandle.AddrOfPinnedObject();
                    var sizeOfT = Marshal.SizeOf(typeof(T));
                    var destinationPtr = (IntPtr) (dataPointer + startIndex * sizeOfT);
                    var sizeInBytes = ((elementCount == 0) ? data.Length : elementCount) * sizeOfT;

                    var dataBox = d3dDeviceContext.MapSubresource(staging, 0, D3D11MapMode.Read, D3D11MapFlags.None);
                    try
                    {
                        // TODO
                        //
                        // データ取得の場合、RowPitch を気にせずに取得できる。
                        // MipLevels = 1 固定でステージングに複製しているからなのだろうか？

                        SDXUtilities.CopyMemory(destinationPtr, dataBox.DataPointer, sizeInBytes);
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

        void CreateD3D11Texture2DDescription(out D3D11Texture2DDescription result)
        {
            // TODO
            // GenerateMipMaps が分からない。
            // インスタンス化の際に初期データを与えた場合にのみ有効？

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
                BindFlags = D3D11BindFlags.ShaderResource,
                CpuAccessFlags = ResourceHelper.GetD3D11CpuAccessFlags((D3D11ResourceUsage) Usage),
                OptionFlags = D3D11ResourceOptionFlags.None
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
