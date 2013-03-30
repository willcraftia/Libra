#region Using

using System;

#endregion

namespace Libra.Graphics
{
    // XNA 4.0 で定義されたものの中から、対応の分かるもののみを列挙。
    // 利用したいフォーマットが現れたら、その都度追加する。
    // DXGI との対応付けと相互変換のため、対応の重複は認めない。

    // なお、None は DXGI における Unknown に対応付けているが、
    // その意味はフォーマット不明ではないため、
    // DXGI Format へ型変換する際には注意が必要。

    // DXGI Format からの DepthFormat への型変換では、
    // enum は未定義であっても数値として強制変換してしまうため、
    // DepthFormat へ対応している型であるかどうかを事前に検査しておく必要がある。
    //
    // ※ただし、内部処理限定。外部は DepthFormat がインタフェースとなるため、
    // 意図的な強制変換を試みない限り問題にはならないと判断。

    public enum DepthFormat
    {
        // 深度バッファとステンシルを用いない。
        None            = 0,

        // DXGI_FORMAT_D16_UNORM
        Depth16         = 55,
        
        // DXGI_FORMAT_D24_UNORM_S8_UINT
        Depth24Stencil8 = 45
    }
}
