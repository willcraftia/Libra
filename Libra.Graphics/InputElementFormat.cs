#region Using

using System;

#endregion

namespace Libra.Graphics
{
    // XNA では VertexElementFormat に対応。
    // DXGI との対応付けと相互変換のため、対応の重複は認めない。

    // DXGI Format からの InputElementFormat への型変換では、
    // enum は未定義であっても数値として強制変換してしまうため、
    // InputElementFormat へ対応している型であるかどうかを事前に検査しておく必要がある。
    //
    // ※ただし、内部処理限定。外部は InputElementFormat がインタフェースとなるため、
    // 意図的な強制変換を試みない限り問題にはならないと判断。

    public enum InputElementFormat
    {
        // DXGI_FORMAT_R8G8B8A8_UNORM
        Color               = 28,

        // DXGI_FORMAT_R32_FLOAT
        Single              = 41,

        // DXGI_FORMAT_R32G32_FLOAT
        Vector2             = 16,

        // DXGI_FORMAT_R32G32B32_FLOAT
        Vector3             = 6,

        // DXGI_FORMAT_R32G32B32A32_FLOAT
        Vector4             = 2,

        // DXGI_FORMAT_R8G8B8A8_UINT
        Byte4               = 30,

        // DXGI_FORMAT_R16G16_SINT
        Short2              = 38,

        // DXGI_FORMAT_R16G16B16A16_SINT
        Short4              = 14,

        // DXGI_FORMAT_R16G16_SNORM
        NormalizedShort2    = 37,

        // DXGI_FORMAT_R16G16B16A16_SNORM
        NormalizedShort4    = 13,

        // DXGI_FORMAT_R16G16_FLOAT
        HalfVector2         = 34,

        // DXGI_FORMAT_R16G16B16A16_FLOAT
        HalfVector4         = 10,
    }
}
