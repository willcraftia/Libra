#region Using

using System;
using Libra;
using Libra.Graphics;

#endregion

namespace Samples.Primitives3D
{
    public sealed class TorusPrimitive : GeometricPrimitive
    {
        public TorusPrimitive(Device device)
            : this(device, 1, 0.333f, 32)
        {
        }

        public TorusPrimitive(Device device, float diameter, float thickness, int tessellation)
            : base(device)
        {
            if (tessellation < 3) throw new ArgumentOutOfRangeException("tessellation");

            for (int i = 0; i < tessellation; i++)
            {
                float outerAngle = i * MathHelper.TwoPi / tessellation;

                Matrix transform = Matrix.CreateTranslation(diameter / 2, 0, 0) *
                                   Matrix.CreateRotationY(outerAngle);

                for (int j = 0; j < tessellation; j++)
                {
                    float innerAngle = j * MathHelper.TwoPi / tessellation;

                    float dx = (float) Math.Cos(innerAngle);
                    float dy = (float) Math.Sin(innerAngle);

                    Vector3 normal = new Vector3(dx, dy, 0);
                    Vector3 position = normal * thickness / 2;

                    position = Vector3.Transform(position, transform);
                    normal = Vector3.TransformNormal(normal, transform);

                    AddVertex(position, normal);

                    int nextI = (i + 1) % tessellation;
                    int nextJ = (j + 1) % tessellation;

                    AddIndex(i * tessellation + j);
                    AddIndex(i * tessellation + nextJ);
                    AddIndex(nextI * tessellation + j);

                    AddIndex(i * tessellation + nextJ);
                    AddIndex(nextI * tessellation + nextJ);
                    AddIndex(nextI * tessellation + j);
                }
            }

            InitializePrimitive();
        }
    }
}
