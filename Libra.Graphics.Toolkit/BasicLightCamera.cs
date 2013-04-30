#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class BasicLightCamera : LightCamera
    {
        Vector3[] corners;

        public BasicLightCamera()
        {
            corners = new Vector3[8];
        }

        protected override void Update()
        {
            var target = eyePosition + LightDirection;
            Matrix.CreateLookAt(ref eyePosition, ref target, ref eyeDirection, out View);

            // シーン領域をライト空間へ変換。
            sceneBox.GetCorners(corners);
            for (int i = 0; i < corners.Length; i++)
                Vector3.TransformCoordinate(ref corners[i], ref View, out corners[i]);

            // ライト空間におけるシーン領域の AABB。
            BoundingBox lightBox;
            BoundingBox.CreateFromPoints(corners, out lightBox);

            // その AABB が収まるように正射影。
            Matrix.CreateOrthographicOffCenter(
                lightBox.Min.X, lightBox.Max.X,
                lightBox.Min.Y, lightBox.Max.Y,
                -lightBox.Max.Z, -lightBox.Min.Z,
                out Projection);
        }
    }
}
