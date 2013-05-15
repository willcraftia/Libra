#region Using

using System;

#endregion

namespace Libra.Graphics
{
    // XNA 4.0 で定義されたものの中から、対応の分かるもののみを列挙。
    // 利用したいフォーマットが現れたら、その都度追加する。
    // DXGI との対応付けと相互変換のため、対応の重複は認めない。

    // DXGI Format からの SurfaceFormat への型変換では、
    // enum は未定義であっても数値として強制変換してしまうため、
    // SurfaceFormat へ対応している型であるかどうかを事前に検査しておく必要がある。
    //
    // ※ただし、内部処理限定。外部は SurfaceFormat がインタフェースとなるため、
    // 意図的な強制変換を試みない限り問題にはならないと判断。

    public enum SurfaceFormat
    {
        // TODO
        // DXGI の定義に沿うべきか否か悩み中。

        //Unknown                     = 0,
        //R32G32B32A32_Typeless       = 1,
        //R32G32B32A32_Float          = 2,
        //R32G32B32A32_UInt           = 3,
        //R32G32B32A32_SInt           = 4,
        //R32G32B32_Typeless          = 5,
        //R32G32B32_Float             = 6,
        //R32G32B32_UInt              = 7,
        //R32G32B32_SInt              = 8,
        //R16G16B16A16_Typeless       = 9,
        //R16G16B16A16_Float          = 10,
        //R16G16B16A16_UNorm          = 11,
        //R16G16B16A16_UInt           = 12,
        //R16G16B16A16_SNorm          = 13,
        //R16G16B16A16_SInt           = 14,
        //R32G32_Typeless             = 15,
        //R32G32_Float                = 16,
        //R32G32_UInt                 = 17,
        //R32G32_SInt                 = 18,
        //R32G8X24_Typeless           = 19,
        //D32_Float_S8X24_UInt        = 20,
        //R32_Float_X8X24_Typeless    = 21,
        //X32_Typeless_G8X24_UInt     = 22,
        //R10G10B10A2_Typeless        = 23,
        //R10G10B10A2_UNorm           = 24,
        //R10G10B10A2_UInt            = 25,
        //R11G11B10_Float             = 26,
        //R8G8B8A8_Typeless           = 27,
        //R8G8B8A8_UNorm              = 28,
        //R8G8B8A8_UNorm_SRGB         = 29,
        //R8G8B8A8_UInt               = 30,
        //R8G8B8A8_SNorm              = 31,
        //R8G8B8A8_SInt               = 32,
        //R16G16_Typeless             = 33,
        //R16G16_Float                = 34,
        //R16G16_UNorm                = 35,
        //R16G16_UInt                 = 36,
        //R16G16_SNorm                = 37,
        //R16G16_SInt                 = 38,
        //R32_Typeless                = 39,
        //D32_Float                   = 40,
        //R32_Float                   = 41,
        //R32_UInt                    = 42,
        //R32_SInt                    = 43,
        //R24G8_Typeless              = 44,
        //D24_UNorm_S8_UInt           = 45,
        //R24_UNorm_X8_Typeless       = 46,
        //X24_Typeless_G8_UInt        = 47,
        //R8G8_Typeless               = 48,
        //R8G8_UNorm                  = 49,
        //R8G8_UInt                   = 50,
        //R8G8_SNorm                  = 51,
        //R8G8_SInt                   = 52,
        //R16_Typeless                = 53,
        //R16_Float                   = 54,
        //D16_UNorm                   = 55,
        //R16_UNorm                   = 56,
        //R16_UInt                    = 57,
        //R16_SNorm                   = 58,
        //R16_SInt                    = 59,
        //R8_Typeless                 = 60,
        //R8_UNorm                    = 61,
        //R8_UInt                     = 62,
        //R8_SNorm                    = 63,
        //R8_SInt                     = 64,
        //A8_UNorm                    = 65,
        //R1_UNorm                    = 66,
        //R9G9B9E5_SHAREDEXP          = 67,
        //R8G8_B8G8_UNorm             = 68,
        //G8R8_G8B8_UNorm             = 69,
        //BC1_Typeless                = 70,
        //BC1_UNorm                   = 71,
        //BC1_UNorm_SRGB              = 72,
        //BC2_Typeless                = 73,
        //BC2_UNorm                   = 74,
        //BC2_UNorm_SRGB              = 75,
        //BC3_Typeless                = 76,
        //BC3_UNorm                   = 77,
        //BC3_UNorm_SRGB              = 78,
        //BC4_Typeless                = 79,
        //BC4_UNorm                   = 80,
        //BC4_SNorm                   = 81,
        //BC5_Typeless                = 82,
        //BC5_UNorm                   = 83,
        //BC5_SNorm                   = 84,
        //B5G6R5_UNorm                = 85,
        //B5G5R5A1_UNorm              = 86,
        //B8G8R8A8_UNorm              = 87,
        //B8G8R8X8_UNorm              = 88,
        //R10G10B10_XR_BIAS_A2_UNorm  = 89,
        //B8G8R8A8_Typeless           = 90,
        //B8G8R8A8_UNorm_SRGB         = 91,
        //B8G8R8X8_Typeless           = 92,
        //B8G8R8X8_UNorm_SRGB         = 93,
        //BC6H_Typeless               = 94,
        //BC6H_UF16                   = 95,
        //BC6H_SF16                   = 96,
        //BC7_Typeless                = 97,
        //BC7_UNorm                   = 98,
        //BC7_UNorm_SRGB              = 99,
        
        // DXGI_FORMAT_R8G8B8A8_UNORM
        Color           = 28,

        // DXGI_FORMAT_B5G6R5_UNORM
        Bgr565          = 85,

        // DXGI_FORMAT_B5G5R5A1_UNORM
        Bgra5551        = 86,

        // 注意
        //
        // SharpDX の DXGI Format で未定義。
        // MSDN によると、DXGI_FORMAT_B4G4R4A4_UNORM は D3D 11.1 (DXGI 1.2) から正式に対応されるとあり、
        // SharpDX では D3D 11 対応の自動生成コードから除外していると推測される。
        // このため、SharpDX の DXGI Format へのキャストでは、
        // 対応する項目名が不明となる。
        // ただし、数値としてキャストは成功するため、
        // そのまま D3D へ渡して動作させる事は可能であると思われる。
        //
        // TODO
        // そう思って強引に渡したらエラーで弾かれた。
        Bgra4444        = 115,

        // DXGI_FORMAT_BC1_UNORM
        // D3D9 における Dxt1
        BC1             = 71,

        // DXGI_FORMAT_BC2_UNORM
        // D3D9 における Dxt3
        BC2             = 74,

        // DXGI_FORMAT_BC3_UNORM
        // D3D9 における Dxt5
        BC3             = 77,

        // DXGI_FORMAT_R8G8_SNORM
        NormalizedByte2 = 51,

        // DXGI_FORMAT_R8G8B8A8_SNORM
        NormalizedByte4 = 31,

        // DXGI_FORMAT_R10G10B10A2_UNORM
        Rgba1010102     = 24,

        // DXGI_FORMAT_R16G16_UNORM
        Rg32            = 35,

        // DXGI_FORMAT_R16G16B16A16_UNORM
        Rgba64          = 11,

        // DXGI_FORMAT_A8_UNORM
        Alpha8          = 65,

        // DXGI_FORMAT_R32_FLOAT
        Single          = 41,

        // DXGI_FORMAT_R32G32_FLOAT
        Vector2         = 16,

        // DXGI_FORMAT_R32G32B32A32_FLOAT
        Vector4         = 2,

        // DXGI_FORMAT_R16_FLOAT
        HalfSingle      = 54,

        // DXGI_FORMAT_R16G16_FLOAT
        HalfVector2     = 34,

        // DXGI_FORMAT_R16G16B16A16_FLOAT
        HalfVector4     = 10,

        // TODO: 対応が分からない。
        //HdrBlendable = 19,
    }
}
