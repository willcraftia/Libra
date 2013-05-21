#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class TorusMesh : PrimitiveMesh
    {
        public TorusMesh(DeviceContext context, float diameter = 1.0f, float thickness = 0.333f, int tessellation = 32)
            : base(context)
        {
            if (diameter <= 0.0f) throw new ArgumentOutOfRangeException("diameter");
            if (thickness <= 0.0f) throw new ArgumentOutOfRangeException("thickness");
            if (tessellation < 3) throw new ArgumentOutOfRangeException("tessellation");

            var vertexCount = tessellation * tessellation;
            var indexCount = vertexCount * 6;

            Allocate(vertexCount, indexCount);
            
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

            Build();
        }
    }
}
