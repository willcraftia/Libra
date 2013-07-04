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

            Width = description.Width;
            Height = description.Height;
            MipLevels = description.MipLevels;
            Format = (SurfaceFormat) description.Format;
            MultisampleCount = description.SampleDescription.Count;
            MultisampleQuality = description.SampleDescription.Quality;
            Usage = (ResourceUsage) description.Usage;
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
                ArraySize = ArraySize,
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
