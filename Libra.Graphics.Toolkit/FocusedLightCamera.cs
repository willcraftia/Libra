#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    public class FocusedLightCamera : LightCamera
    {
        // 光の方向 (光源からの光の進行方向)
        public Vector3 LightDirection;

        protected List<Vector3> lightVolumePoints;

        Vector3[] corners;

        public FocusedLightCamera()
        {
            LightDirection = Vector3.Down;
            LightViewProjection = Matrix.Identity;
            corners = new Vector3[BoundingBox.CornerCount];
            lightVolumePoints = new List<Vector3>();
        }

        public void AddLightVolumePoint(Vector3 point)
        {
            lightVolumePoints.Add(point);
        }

        public void AddLightVolumePoint(ref Vector3 point)
        {
            lightVolumePoints.Add(point);
        }

        public void AddLightVolumePoints(IEnumerable<Vector3> points)
        {
            if (points == null) throw new ArgumentNullException("points");

            foreach (var point in points)
                lightVolumePoints.Add(point);
        }

        public void AddLightVolumePoints(BoundingBox box)
        {
            AddLightVolumePoints(ref box);
        }

        public void AddLightVolumePoints(ref BoundingBox box)
        {
            box.GetCorners(corners);
            AddLightVolumePoints(corners);
        }

        public void AddLightVolumePoints(BoundingFrustum frustum)
        {
            frustum.GetCorners(corners);
            AddLightVolumePoints(corners);
        }

        public override void Update()
        {
            // USM (Uniform Shadow Mapping) による行列算出。

            var position = Vector3.Zero;
            var target = LightDirection;
            var up = Vector3.Up;

            // 仮光源ビュー行列。
            Matrix tempLightView;
            Matrix.CreateLookAt(ref position, ref target, ref up, out tempLightView);

            // 指定されている点を含む境界ボックス。
            BoundingBox tempLightBox;
            BoundingBox.CreateFromPoints(lightVolumePoints, out tempLightBox);

            // 境界ボックスの頂点を仮光源空間へ変換。
            tempLightBox.GetCorners(corners);
            for (int i = 0; i < corners.Length; i++)
                Vector3.Transform(ref corners[i], ref tempLightView, out corners[i]);

            // 仮光源空間での境界ボックス。
            BoundingBox lightBox;
            BoundingBox.CreateFromPoints(corners, out lightBox);

            Vector3 boxSize;
            Vector3.Subtract(ref lightBox.Max, ref lightBox.Min, out boxSize);

            Vector3 halfBoxSize;
            Vector3.Multiply(ref boxSize, 0.5f, out halfBoxSize);

            // 光源から見て最も近い面 (Min.Z) の中心 (XY について半分の位置) を光源位置に決定。
            Vector3 lightPosition;
            Vector3.Add(ref lightBox.Min, ref halfBoxSize, out lightPosition);
            lightPosition.Z = lightBox.Min.Z;

            // 算出した光源位置は仮光源空間にあるため、これをワールド空間へ変換。
            Matrix lightViewInv;
            Matrix.Invert(ref tempLightView, out lightViewInv);
            Vector3.Transform(ref lightPosition, ref lightViewInv, out lightPosition);

            Vector3.Add(ref lightPosition, ref LightDirection, out target);

            // 得られた光源情報から光源ビュー行列を算出。
            Matrix.CreateLookAt(ref lightPosition, ref target, ref up, out LightView);

            // 仮光源空間の境界ボックスのサイズで正射影として光源射影行列を算出。
            Matrix.CreateOrthographic(boxSize.X, boxSize.Y, -boxSize.Z, boxSize.Z, out LightProjection);

            Matrix.Multiply(ref LightView, ref LightProjection, out LightViewProjection);

            // クリア。
            lightVolumePoints.Clear();
        }
    }
}
