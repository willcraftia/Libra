#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public struct VertexBufferBinding
    {
        public VertexBuffer VertexBuffer;

        // バイト単位でのオフセット。
        // ModelMeshPart の VertexOffset は、
        // XNA 仕様に合わせて頂点数単位でのオフセットであるため差異に注意。
        // ModelMeshPart の VertexOffset は、頂点ストライドを掛ける事でバイト単位となる。
        public int Offset;

        public VertexBufferBinding(VertexBuffer vertexBuffer, int offset = 0)
        {
            VertexBuffer = vertexBuffer;
            Offset = offset;
        }
    }
}
