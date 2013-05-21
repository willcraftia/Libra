#region Using

using System;

#endregion

// Xbox LIVE Indie Games - Primitives3D より移植
// http://xbox.create.msdn.com/en-US/education/catalog/sample/primitives_3d

namespace Libra.Graphics.Toolkit
{
    public sealed class CubeMesh : PrimitiveMesh
    {
        static readonly Vector3[] normals =
        {
            Vector3.Up,
            Vector3.Down,
            Vector3.Forward,
            Vector3.Backward,
            Vector3.Left,
            Vector3.Right
        };

        public CubeMesh(DeviceContext context, float size = 1.0f)
            : base(context)
        {
            if (size <= 0.0f) throw new ArgumentOutOfRangeException("size");

            const int vertexCount = 6 * 4;
            const int indexCount = 6 * 6;
            
            Allocate(vertexCount, indexCount);

            foreach (Vector3 normal in normals)
            {
                var side1 = new Vector3(normal.Y, normal.Z, normal.X);
                var side2 = Vector3.Cross(normal, side1);

                AddIndex(CurrentVertex + 0);
                AddIndex(CurrentVertex + 1);
                AddIndex(CurrentVertex + 2);

                AddIndex(CurrentVertex + 0);
                AddIndex(CurrentVertex + 2);
                AddIndex(CurrentVertex + 3);

                AddVertex((normal - side1 - side2) * size / 2, normal);
                AddVertex((normal - side1 + side2) * size / 2, normal);
                AddVertex((normal + side1 + side2) * size / 2, normal);
                AddVertex((normal + side1 - side2) * size / 2, normal);
            }

            Build();
        }
    }
}
