#region Using

using System;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Viewport
    {
        public float X;

        public float Y;

        public float Width;

        public float Height;

        public float MinDepth;

        public float MaxDepth;

        public float AspectRatio
        {
            get
            {
                if (!MathHelper.WithinEpsilon(Height, 0f))
                {
                    return Width / Height;
                }
                return 0f;
            }
        }

        public Viewport(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            MinDepth = 0f;
            MaxDepth = 1f;
        }

        public Viewport(float x, float y, float width, float height, float minDepth, float maxDepth)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            MinDepth = minDepth;
            MaxDepth = maxDepth;
        }

        public Vector3 Project(Vector3 source, Matrix projection, Matrix view, Matrix world)
        {
            var matrix = Matrix.Multiply(Matrix.Multiply(world, view), projection);
            var vector = (Vector3) Vector3.Transform(source, matrix);
            float a = (((source.X * matrix.M14) + (source.Y * matrix.M24)) + (source.Z * matrix.M34)) + matrix.M44;

            if (!MathHelper.WithinEpsilon(a, 1f))
            {
                vector = (vector / a);
            }

            vector.X = (vector.X + 1f) * 0.5f * Width + X;
            vector.Y = (-vector.Y + 1f) * 0.5f * Height + Y;
            vector.Z = vector.Z * (MaxDepth - MinDepth) + MinDepth;

            return vector;
        }

        public Vector3 Unproject(Vector3 source, Matrix projection, Matrix view, Matrix world)
        {
            var matrix = Matrix.Invert(Matrix.Multiply(Matrix.Multiply(world, view), projection));
            source.X = (source.X - X) / Width * 2f - 1f;
            source.Y = -((source.Y - Y) / Height * 2f - 1f);
            source.Z = (source.Z - MinDepth) / (MaxDepth - MinDepth);
            var vector = (Vector3) Vector3.Transform(source, matrix);

            float a = source.X * matrix.M14 + source.Y * matrix.M24 + source.Z * matrix.M34 + matrix.M44;
            if (!MathHelper.WithinEpsilon(a, 1f))
            {
                vector = (vector / a);
            }

            return vector;
        }

        #region ToString

        public override string ToString()
        {
            return "{X:" + X + " Y:" + Y + " Width:" + Width + " Height:" + Height +
                " MinDepth:" + MinDepth + " MaxDepth:" + MaxDepth + "}";
        }

        #endregion
    }
}
