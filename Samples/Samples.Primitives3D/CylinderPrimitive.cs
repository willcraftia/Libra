#region Using

using System;
using Libra;
using Libra.Graphics;

#endregion

namespace Samples.Primitives3D
{
    public sealed class CylinderPrimitive : GeometricPrimitive
    {
        public CylinderPrimitive(IDevice device)
            : this(device, 1, 1, 32)
        {
        }

        public CylinderPrimitive(IDevice device, float height, float diameter, int tessellation)
            : base(device)
        {
            if (tessellation < 3) throw new ArgumentOutOfRangeException("tessellation");

            height /= 2;

            float radius = diameter / 2;

            for (int i = 0; i < tessellation; i++)
            {
                Vector3 normal = GetCircleVector(i, tessellation);

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

            InitializePrimitive();
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
                Vector3 position = GetCircleVector(i, tessellation) * radius +
                                   normal * height;

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
