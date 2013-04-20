#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class BasicLightCamera : LightCamera
    {
        // 光の方向 (光源からの光の進行方向)
        public Vector3 LightDirection;

        public Matrix EyeView;

        public Matrix EyeProjection;

        BoundingFrustum eyeFrustum;

        Vector3[] corners;

        public BasicLightCamera()
        {
            EyeView = Matrix.Identity;
            EyeProjection = Matrix.Identity;
            eyeFrustum = new BoundingFrustum(Matrix.Identity);
            corners = new Vector3[8];
        }

        public override void Update()
        {
            // 表示カメラのビュー行列の逆行列 (表示カメラのワールド変換)。
            Matrix eyeTransform;
            Matrix.Invert(ref EyeView, out eyeTransform);

            // 表示カメラの位置と方向の抽出。
            var eyePosition = eyeTransform.Translation;
            var eyeDirection = eyeTransform.Forward;

            var target = eyePosition + LightDirection;
            Matrix.CreateLookAt(ref eyePosition, ref target, ref eyeDirection, out LightView);

            // 表示カメラ境界錐台の更新。
            Matrix eyeViewProjection;
            Matrix.Multiply(ref EyeView, ref EyeProjection, out eyeViewProjection);
            eyeFrustum.Matrix = eyeViewProjection;

            // 表示カメラ境界錐台の AABB をシーン領域として算出。
            BoundingBox tempLightBox;
            eyeFrustum.GetCorners(corners);
            BoundingBox.CreateFromPoints(corners, out tempLightBox);

            // シーン領域を表示カメラ空間へ変換。
            tempLightBox.GetCorners(corners);
            for (int i = 0; i < corners.Length; i++)
                Vector3.Transform(ref corners[i], ref LightView, out corners[i]);

            // 表示カメラ空間におけるシーン領域の AABB。
            BoundingBox lightBox;
            BoundingBox.CreateFromPoints(corners, out lightBox);

            // その AABB が収まるように正射影。
            Matrix.CreateOrthographicOffCenter(
                lightBox.Min.X, lightBox.Max.X,
                lightBox.Min.Y, lightBox.Max.Y,
                -lightBox.Max.Z, -lightBox.Min.Z,
                out LightProjection);

            Matrix.Multiply(ref LightView, ref LightProjection, out LightViewProjection);
        }
    }
}
