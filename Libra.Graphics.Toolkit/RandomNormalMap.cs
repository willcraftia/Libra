#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class RandomNormalMap
    {
        public static Texture2D Create(
            DeviceContext context, Random random, int width, int height, SurfaceFormat format = SurfaceFormat.Vector4)
        {
            var normals = new Vector4[width * height];
            for (int i = 0; i < normals.Length; i++)
            {
                var normal = new Vector4
                {
                    X = (float) random.NextDouble() * 2.0f - 1.0f,
                    Y = (float) random.NextDouble() * 2.0f - 1.0f,
                    Z = (float) random.NextDouble() * 2.0f - 1.0f,
                    W = 0
                };
                normal.Normalize();

                normal.X = normal.X * 0.5f + 0.5f;
                normal.Y = normal.Y * 0.5f + 0.5f;
                normal.Z = normal.Z * 0.5f + 0.5f;

                normals[i] = normal;
            }

            var texture = context.Device.CreateTexture2D();
            texture.Width = width;
            texture.Height = height;
            texture.Format = format;
            texture.Initialize();
            texture.SetData(context, normals);

            return texture;
        }
    }
}
