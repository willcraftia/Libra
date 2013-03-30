#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public sealed class ModelMeshPart
    {
        public VertexBuffer VertexBuffer { get; set; }

        public IndexBuffer IndexBuffer { get; set; }

        public InputLayout InputLayout { get; set; }

        public int BaseVertexLocation { get; set; }

        public int VertexCount { get; set; }

        public int StartIndexLocation { get; set; }

        public int IndexCount { get; set; }

        public IEffect Effect { get; set; }
    }
}
