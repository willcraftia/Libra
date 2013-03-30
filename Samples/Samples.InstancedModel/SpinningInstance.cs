#region Using

using System;
using Libra;
using Libra.Games;

#endregion

namespace Samples.InstancedModel
{
    public sealed class SpinningInstance
    {
        float size;

        float spiralSpeed;
        
        float spinSpeed;
        
        Vector3 spinAxis;
        
        public SpinningInstance()
        {
            size = RandomNumberBetween(0, 1);
            spiralSpeed = RandomNumberBetween(-1, 1);
            spinSpeed = RandomNumberBetween(-2, 2);

            spinAxis.X = RandomNumberBetween(-1, 1);
            spinAxis.Y = RandomNumberBetween(-1, 1);
            spinAxis.Z = RandomNumberBetween(-1, 1);

            if (spinAxis.LengthSquared() > 0.001f)
                spinAxis.Normalize();
            else
                spinAxis = Vector3.Up;
        }

        public Matrix Transform
        {
            get { return transform; }
        }

        Matrix transform;

        public void Update(GameTime gameTime)
        {
            float time = (float) gameTime.TotalGameTime.TotalSeconds;

            Matrix scale, rotation;

            Matrix.CreateScale(size, out scale);
            Matrix.CreateFromAxisAngle(ref spinAxis, spinSpeed * time, out rotation);

            Matrix.Multiply(ref scale, ref rotation, out transform);

            float spiralTime = time * spiralSpeed;

            float spiralSize = (float) Math.Sin(spiralTime / 4) * 4;

            transform.M41 = (float) Math.Cos(spiralTime) * spiralSize;
            transform.M42 = (float) Math.Sin(spiralTime) * spiralSize;
            transform.M43 = (float) Math.Sin(spiralTime / 3) * 6;
        }

        static float RandomNumberBetween(float min, float max)
        {
            return MathHelper.Lerp(min, max, (float) random.NextDouble());
        }

        static Random random = new Random();
    }
}
