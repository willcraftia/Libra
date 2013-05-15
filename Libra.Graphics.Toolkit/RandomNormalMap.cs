#region Using

using System;
using Libra.PackedVector;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class RandomNormalMap
    {
        public static Texture2D CreateAsR8G8B8A8SNorm(DeviceContext context, Random random, int width, int height)
        {
            var normals = new NormalizedByte4[width * height];
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

                normals[i] = new NormalizedByte4(normal);
            }

            var texture = context.Device.CreateTexture2D();
            texture.Width = width;
            texture.Height = height;
            texture.Format = SurfaceFormat.NormalizedByte4;
            texture.Initialize();
            texture.SetData(context, normals);

            return texture;
        }
    }
}
