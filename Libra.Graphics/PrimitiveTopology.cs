#region Using

using System;

#endregion

namespace Libra.Graphics
{
    // ひとまず XNA にあるもののみを定義。
    // 他は、利用の機会が有れば、その都度追加。

    public enum PrimitiveTopology
    {
        // D3D_PRIMITIVE_TOPOLOGY_LINELIST
        LineList        = 2,

        // D3D_PRIMITIVE_TOPOLOGY_LINESTRIP
        LineStrip       = 3,

        // D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST
        TriangleList    = 4,

        // D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP
        TriangleStrip   = 5,
    }
}
