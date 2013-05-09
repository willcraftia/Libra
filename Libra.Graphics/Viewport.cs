#region Using

using System;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
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
            get { return Width / Height; }
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
            Vector3 result;
            Project(ref source, ref projection, ref view, ref world, out result);
            return result;
        }

        public void Project(ref Vector3 source, ref Matrix projection, ref Matrix view, ref Matrix world, out Vector3 result)
        {
            Matrix worldView;
            Matrix.Multiply(ref world, ref view, out worldView);

            Matrix transform;
            Matrix.Multiply(ref worldView, ref projection, out transform);

            Vector3.TransformCoordinate(ref source, ref transform, out result);

            result.X = (result.X + 1f) * 0.5f * Width + X;
            result.Y = (-result.Y + 1f) * 0.5f * Height + Y;
            result.Z = result.Z * (MaxDepth - MinDepth) + MinDepth;
        }

        public Vector3 Unproject(Vector3 source, Matrix projection, Matrix view, Matrix world)
        {
            Vector3 result;
            Unproject(ref source, ref projection, ref view, ref world, out result);
            return result;
        }

        public void Unproject(ref Vector3 source, ref Matrix projection, ref Matrix view, ref Matrix world, out Vector3 result)
        {
            Vector3 adjustedSource = source;
            adjustedSource.X = (adjustedSource.X - X) / Width * 2f - 1f;
            adjustedSource.Y = -((adjustedSource.Y - Y) / Height * 2f - 1f);
            adjustedSource.Z = (adjustedSource.Z - MinDepth) / (MaxDepth - MinDepth);

            Matrix worldView;
            Matrix.Multiply(ref world, ref view, out worldView);

            Matrix transform;
            Matrix.Multiply(ref worldView, ref projection, out transform);

            Matrix invertTransform;
            Matrix.Invert(ref transform, out invertTransform);

            Vector3.TransformCoordinate(ref adjustedSource, ref invertTransform, out result);
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
