#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class SquareMesh : PrimitiveMesh
    {
        public SquareMesh(Device device, float size = 1.0f)
            : base(device)
        {
            if (size < 0.0f) throw new ArgumentOutOfRangeException("size");

            const int vertexCount = 4;
            const int indexCount = 6;

            Allocate(vertexCount, indexCount);

            float halfSize = size * 0.5f;

            AddVertex(new Vector3(-halfSize, 0,  halfSize), Vector3.Up);
            AddVertex(new Vector3(-halfSize, 0, -halfSize), Vector3.Up);
            AddVertex(new Vector3( halfSize, 0, -halfSize), Vector3.Up);
            AddVertex(new Vector3( halfSize, 0,  halfSize), Vector3.Up);

            AddIndex(0);
            AddIndex(1);
            AddIndex(2);

            AddIndex(0);
            AddIndex(2);
            AddIndex(3);

            Build();
        }
    }
}
