#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class LightCamera
    {
        public Matrix LightViewProjection;

        Vector3[] corners;

        List<Vector3> lightVolumePoints;

        public LightCamera()
        {
            LightViewProjection = Matrix.Identity;
            corners = new Vector3[BoundingBox.CornerCount];
            lightVolumePoints = new List<Vector3>();
        }

        public void AddLightVolumePoint(ref Vector3 point)
        {
            lightVolumePoints.Add(point);
        }

        public void AddLightVolumePoints(Vector3[] points)
        {
            if (points == null) throw new ArgumentNullException("points");

            for (int i = 0; i < points.Length; i++)
                lightVolumePoints.Add(points[i]);
        }

        public void Update(Vector3 lightDirection)
        {
            var position = Vector3.Zero;
            var target = lightDirection;
            var up = Vector3.Up;

            Matrix tempLightView;
            Matrix.CreateLookAt(ref position, ref target, ref up, out tempLightView);

            BoundingBox tempLightVolume;
            BoundingBox.CreateFromPoints(lightVolumePoints, out tempLightVolume);

            tempLightVolume.GetCorners(corners);
            for (int i = 0; i < corners.Length; i++)
                Vector3.Transform(ref corners[i], ref tempLightView, out corners[i]);

            BoundingBox lightVolume;
            BoundingBox.CreateFromPoints(corners, out lightVolume);

            Vector3 boxSize;
            Vector3.Subtract(ref lightVolume.Max, ref lightVolume.Min, out boxSize);
            Vector3 halfBoxSize;
            Vector3.Multiply(ref boxSize, 0.5f, out halfBoxSize);

            // 仮ライト空間での仮光源位置を算出。
            Vector3 lightPosition;
            Vector3.Add(ref lightVolume.Min, ref halfBoxSize, out lightPosition);
            lightPosition.Z = lightVolume.Min.Z;

            // 仮光源位置を仮ライト空間からワールド空間へ変換。
            Matrix lightViewInv;
            Matrix.Invert(ref tempLightView, out lightViewInv);
            Vector3.Transform(ref lightPosition, ref lightViewInv, out lightPosition);

            Vector3.Add(ref lightPosition, ref lightDirection, out target);

            Matrix lightView;
            Matrix.CreateLookAt(ref lightPosition, ref target, ref up, out lightView);

            // REFERECE: http://msdn.microsoft.com/ja-jp/library/ee416324(VS.85).aspx

            //float bound = boxSize.Z;
            //float unitPerTexel = bound / shadowMapSize;

            //boxSize.X /= unitPerTexel;
            //boxSize.X = MathExtension.Floor(boxSize.X);
            //boxSize.X *= unitPerTexel;

            //boxSize.Y /= unitPerTexel;
            //boxSize.Y = MathExtension.Floor(boxSize.Y);
            //boxSize.Y *= unitPerTexel;

            //boxSize.Z /= unitPerTexel;
            //boxSize.Z = MathExtension.Floor(boxSize.Z);
            //boxSize.Z *= unitPerTexel;

            Matrix lightProjection;
            Matrix.CreateOrthographic(boxSize.X, boxSize.Y, -boxSize.Z, boxSize.Z, out lightProjection);

            Matrix.Multiply(ref lightView, ref lightProjection, out LightViewProjection);

            // クリア。
            lightVolumePoints.Clear();
        }
    }
}
