#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class CylinderMesh : PrimitiveMesh
    {
        public CylinderMesh(DeviceContext context, float height = 1.0f, float diameter = 1.0f, int tessellation = 32)
            : base(context)
        {
            if (height <= 0.0f) throw new ArgumentOutOfRangeException("height");
            if (diameter <= 0.0f) throw new ArgumentOutOfRangeException("diameter");
            if (tessellation < 3) throw new ArgumentOutOfRangeException("tessellation");

            var vertexCount = tessellation * 2 + tessellation * 2;
            var indexCount = tessellation * 6 + (tessellation - 2) * 3 * 2;

            Allocate(vertexCount, indexCount);

            height /= 2;

            float radius = diameter / 2;

            for (int i = 0; i < tessellation; i++)
            {
                var normal = GetCircleVector(i, tessellation);

                AddVertex(normal * radius + Vector3.Up * height, normal);
                AddVertex(normal * radius + Vector3.Down * height, normal);

                AddIndex(i * 2);
                AddIndex(i * 2 + 1);
                AddIndex((i * 2 + 2) % (tessellation * 2));

                AddIndex(i * 2 + 1);
                AddIndex((i * 2 + 3) % (tessellation * 2));
                AddIndex((i * 2 + 2) % (tessellation * 2));
            }

            CreateCap(tessellation, height, radius, Vector3.Up);
            CreateCap(tessellation, height, radius, Vector3.Down);

            Build();
        }

        void CreateCap(int tessellation, float height, float radius, Vector3 normal)
        {
            for (int i = 0; i < tessellation - 2; i++)
            {
                if (normal.Y > 0)
                {
                    AddIndex(CurrentVertex);
                    AddIndex(CurrentVertex + (i + 1) % tessellation);
                    AddIndex(CurrentVertex + (i + 2) % tessellation);
                }
                else
                {
                    AddIndex(CurrentVertex);
                    AddIndex(CurrentVertex + (i + 2) % tessellation);
                    AddIndex(CurrentVertex + (i + 1) % tessellation);
                }
            }

            for (int i = 0; i < tessellation; i++)
            {
                var position = GetCircleVector(i, tessellation) * radius + normal * height;

                AddVertex(position, normal);
            }
        }

        static Vector3 GetCircleVector(int i, int tessellation)
        {
            float angle = i * MathHelper.TwoPi / tessellation;

            float dx = (float) Math.Cos(angle);
            float dz = (float) Math.Sin(angle);

            return new Vector3(dx, 0, dz);
        }
    }
}
