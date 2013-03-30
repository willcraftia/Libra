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
