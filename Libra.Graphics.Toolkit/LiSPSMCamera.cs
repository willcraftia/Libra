#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    public class LiSPSMCamera : LightCamera
    {
        /// <summary>
        /// LSPSM の新しい式が有効かどうかを示します。
        /// true (新しい式が有効な場合)、false (それ以外の場合)。
        /// </summary>
        public bool UseNewNFormula { get; set; }

        /// <summary>
        /// 明示した N 値を使用するかどうかを示します。
        /// true (明示した N 値を使用する場合)、false (それ以外の場合)。
        /// </summary>
        public bool ExplicitNEnabled { get; set; }

        /// <summary>
        /// 明示する N 値です。
        /// </summary>
        public float N { get; set; }

        public Vector3 EyePosition;

        public Vector3 EyeDirection;

        public float EyeNearPlaneDistance;

        List<Vector3> transformedLightVolumePoints;

        public LiSPSMCamera()
        {
            UseNewNFormula = true;
            ExplicitNEnabled = false;
            N = 10.0f;

            transformedLightVolumePoints = new List<Vector3>();
        }

        public override void Update()
        {
            // カメラと光のなす角の算出。
            var L = LightDirection;
            L.Normalize();

            var E = EyeDirection;
            E.Normalize();

            float eDotL;
            Vector3.Dot(ref E, ref L, out eDotL);
            if (1.0f - 0.01f <= Math.Abs(eDotL))
            {
                // カメラと光がほぼ平行ならば外積が縮退するため、
                // USM で行列を算出。
                base.Update();
                return;
            }

            // 光の UP ベクトルの算出。
            Vector3 left;
            Vector3.Cross(ref L, ref E, out left);

            Vector3 up;
            Vector3.Cross(ref left, ref L, out up);
            up.Normalize();

            // 仮光源ビュー行列。
            var tempLightPosition = E;
            var tempLightTarget = tempLightPosition + L;
            // E に拘る必要が無いのでは？
            //var tempLightPosition = Vector3.Zero;
            //var tempLightTarget = L;
            Matrix tempLightView;
            Matrix.CreateLookAt(ref tempLightPosition, ref tempLightTarget, ref up, out tempLightView);

            // 指定されている点を仮光源空間へ変換。
            for (int i = 0; i < lightVolumePoints.Count; i++)
            {
                var source = lightVolumePoints[i];
                
                Vector3 destination;
                Vector3.TransformCoordinate(ref source, ref tempLightView, out destination);
                
                transformedLightVolumePoints.Add(destination);
            }

            // 仮光源空間での指定された点を含む境界ボックス。
            BoundingBox lightBox;
            BoundingBox.CreateFromPoints(transformedLightVolumePoints, out lightBox);

            // 錐台 P の深さ
            float depth = lightBox.Max.Y - lightBox.Min.Y;

            // 錐台 P の near。
            float near;
            if (ExplicitNEnabled)
            {
                near = N;
            }
            else
            {
                float sinGamma = (float) Math.Sqrt(1.0d - eDotL * eDotL);
                var zNear = EyeNearPlaneDistance / sinGamma;

                if (UseNewNFormula)
                {
                    var z0 = -zNear;
                    var z1 = -(zNear + depth * sinGamma);
                    near = depth / ((float) Math.Sqrt(z1 / z0) - 1.0f);
                }
                else
                {
                    var zFar = zNear + depth * sinGamma;
                    near = (zNear + (float) Math.Sqrt(zFar * zNear)) / sinGamma;
                }
            }

            // 錐台 P の far。
            float far = near + depth;

            // 錐台 P の視点位置とビュー行列。
            var pPosition = EyePosition - up * (near - EyeNearPlaneDistance);
            var pTarget = pPosition + L;
            Matrix.CreateLookAt(ref pPosition, ref pTarget, ref up, out LightView);

            // Y に関する射影。
            //
            // a = (far + near) / (far - near)
            // b = -2 * far * near / (far - near)
            //
            // [ 1 0 0 0]
            // [ 0 a 0 1]
            // [ 0 0 1 0]
            // [ 0 b 0 0]
            //
            // オリジナルは GL であり、ベクトルと行列の演算順が異なる点に注意。
            var lispProjection = Matrix.Identity;

            lispProjection.M22 = (far + near) / (far - near);
            lispProjection.M24 = 1;
            lispProjection.M42 = -2.0f * far * near / (far - near);
            lispProjection.M44 = 0;

            // P ビュー×Y 射影行列。
            Matrix tempLightViewProjection;
            Matrix.Multiply(ref LightView, ref lispProjection, out tempLightViewProjection);

            // P ビュー×Y 射影空間へ元の点を変換。
            transformedLightVolumePoints.Clear();
            for (int i = 0; i < lightVolumePoints.Count; i++)
            {
                var source = lightVolumePoints[i];

                Vector3 destination;
                Vector3.TransformCoordinate(ref source, ref tempLightViewProjection, out destination);

                transformedLightVolumePoints.Add(destination);
            }

            // 得られた点を含む境界ボックス。
            BoundingBox.CreateFromPoints(transformedLightVolumePoints, out lightBox);

            // スケール変換行列。
            Matrix scaleTranslation;

            Vector3 boxSize;
            Vector3.Subtract(ref lightBox.Max, ref lightBox.Min, out boxSize);
            Matrix.CreateOrthographic(boxSize.X, boxSize.Y, -boxSize.Z, boxSize.Z, out scaleTranslation);

            // 最終的な射影行列。
            Matrix LightProjection;
            Matrix.Multiply(ref lispProjection, ref scaleTranslation, out LightProjection);

            Matrix.Multiply(ref LightView, ref LightProjection, out LightViewProjection);

            lightVolumePoints.Clear();
            transformedLightVolumePoints.Clear();
        }
    }
}
