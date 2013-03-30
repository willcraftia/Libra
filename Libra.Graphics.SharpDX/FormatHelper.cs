#region Using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using DXGIFormat = SharpDX.DXGI.Format;
using DXGIFormatHelper = SharpDX.DXGI.FormatHelper;

#endregion

namespace Libra.Graphics.SharpDX
{
    public static class FormatHelper
    {
        public static ReadOnlyCollection<DXGIFormat> DXGIFormatsAsSurfaceFormat { get; private set; }

        public static ReadOnlyCollection<DXGIFormat> DXGIFormatsAsDepthFormat { get; private set; }

        static FormatHelper()
        {
            var surfaceFormats = new List<DXGIFormat>();
            var depthFormats = new List<DXGIFormat>();

            surfaceFormats.Add(DXGIFormat.R8G8B8A8_UNorm);
            surfaceFormats.Add(DXGIFormat.B5G6R5_UNorm);
            surfaceFormats.Add(DXGIFormat.B5G5R5A1_UNorm);

            surfaceFormats.Add(DXGIFormat.BC1_UNorm);
            surfaceFormats.Add(DXGIFormat.BC2_UNorm);
            surfaceFormats.Add(DXGIFormat.BC3_UNorm);

            // SharpDX 未定義。
            // DXGI_FORMAT_B4G4R4A4_UNORM は D11.1 で破棄のため、
            // D11.1 ビルド用に自動生成したコードから除外されていると推測される。
            //surfaceFormats.Add(SurfaceFormat.Bgra4444, DXGIFormat.Unknown);

            surfaceFormats.Add(DXGIFormat.R8G8_SNorm);
            surfaceFormats.Add(DXGIFormat.R8G8B8A8_SNorm);

            surfaceFormats.Add(DXGIFormat.R10G10B10A2_UNorm);
            surfaceFormats.Add(DXGIFormat.R16G16_UNorm);
            surfaceFormats.Add(DXGIFormat.R16G16B16A16_UNorm);
            surfaceFormats.Add(DXGIFormat.A8_UNorm);
            surfaceFormats.Add(DXGIFormat.R32_Float);
            surfaceFormats.Add(DXGIFormat.R32G32_Float);
            surfaceFormats.Add(DXGIFormat.R32G32B32A32_Float);
            surfaceFormats.Add(DXGIFormat.R16_Float);
            surfaceFormats.Add(DXGIFormat.R16G16_Float);
            surfaceFormats.Add(DXGIFormat.R16G16B16A16_Float);

            // TODO: 対応が分からない。
            //Register(SurfaceFormat.HdrBlendable, DXGIFormat.Unknown);

            depthFormats.Add(DXGIFormat.D16_UNorm);
            depthFormats.Add(DXGIFormat.D24_UNorm_S8_UInt);

            // 読み取り専用コレクションを生成。
            DXGIFormatsAsSurfaceFormat = new ReadOnlyCollection<DXGIFormat>(surfaceFormats);
            DXGIFormatsAsDepthFormat = new ReadOnlyCollection<DXGIFormat>(depthFormats);
        }

        public static int SizeOfInBytes(SurfaceFormat format)
        {
            return (int) DXGIFormatHelper.SizeOfInBytes((DXGIFormat) format);
        }

        public static int SizeOfInBytes(DepthFormat format)
        {
            return (int) DXGIFormatHelper.SizeOfInBytes((DXGIFormat) format);
        }

        public static int SizeOfInBytes(InputElementFormat format)
        {
            // DXGI の定義で見た場合に、各要素のビット数を見て、
            // 8 ビット = 1 バイトであることを念頭に、
            // 各要素のバイト数の和を求めるのみ。
            // あるいは、各要素のビット数の総和を求め、
            // 8 で割った結果でも同じ。
            // SharpDX.DXGI.FormatHelper は後者の実装。

            return (int) DXGIFormatHelper.SizeOfInBytes((DXGIFormat) format);
        }
    }
}
