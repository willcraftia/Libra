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
        public bool UseExplicitN { get; set; }

        /// <summary>
        /// 明示する N 値です。
        /// </summary>
        public float ExplicitN { get; set; }

        public Vector3 EyePosition;

        public Vector3 EyeDirection;

        public float EyeNearPlaneDistance;

        List<Vector3> transformedLightVolumePoints;

        public LiSPSMCamera()
        {
            UseNewNFormula = true;
            UseExplicitN = false;
            ExplicitN = 10.0f;

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

            // 光の UP ベクトル。
            Vector3 up;
            CalculateLightUp(ref L, ref E, out up);

            // 仮光源ビュー行列。
            var tempLightPosition = E;
            var tempLightTarget = tempLightPosition + L;
            // E に拘る必要が無いのでは？
            //var tempLightPosition = Vector3.Zero;
            //var tempLightTarget = L;
            Matrix tempLightView;
            Matrix.CreateLookAt(ref tempLightPosition, ref tempLightTarget, ref up, out tempLightView);

            // 指定されている点を仮光源空間へ変換。
            TransformLightVolumePoints(ref tempLightView);

            // 仮光源空間での指定された点を含む境界ボックス。
            BoundingBox lightBox;
            BoundingBox.CreateFromPoints(transformedLightVolumePoints, out lightBox);

            // 錐台 P の d (n から f の距離)。
            float d = lightBox.Max.Y - lightBox.Min.Y;

            // 錐台 P の n (近平面)。
            float n = CalculateN(d, eDotL);

            // 錐台 P の f (遠平面)。
            float f = n + d;

            // 錐台 P の視点位置とビュー行列。
            var pPosition = EyePosition - up * (n - EyeNearPlaneDistance);
            var pTarget = pPosition + L;
            Matrix.CreateLookAt(ref pPosition, ref pTarget, ref up, out LightView);

            // Y 方向での n と f による透視変換。
            Matrix lispProjection;
            CreateYPerspective(n, f, out lispProjection);

            // P ビュー×Y 射影行列。
            Matrix tempLightViewProjection;
            Matrix.Multiply(ref LightView, ref lispProjection, out tempLightViewProjection);

            // P ビュー×Y 射影空間における境界ボックス。
            transformedLightVolumePoints.Clear();
            TransformLightVolumePoints(ref tempLightViewProjection);
            BoundingBox.CreateFromPoints(transformedLightVolumePoints, out lightBox);

            // スケール変換行列。
            Matrix scaleTransform;
            Matrix.CreateOrthographicOffCenter(
                lightBox.Min.X, lightBox.Max.X,
                lightBox.Min.Y, lightBox.Max.Y,
                -lightBox.Max.Z, -lightBox.Min.Z,
                out scaleTransform);

            // 最終的な射影行列。
            Matrix LightProjection;
            Matrix.Multiply(ref lispProjection, ref scaleTransform, out LightProjection);

            // ビュー×射影行列。
            Matrix.Multiply(ref LightView, ref LightProjection, out LightViewProjection);

            lightVolumePoints.Clear();
            transformedLightVolumePoints.Clear();
        }

        void CalculateLightUp(ref Vector3 L, ref Vector3 E, out Vector3 result)
        {
            Vector3 left;
            Vector3.Cross(ref L, ref E, out left);

            Vector3.Cross(ref left, ref L, out result);
            result.Normalize();
        }

        float CalculateN(float d, float eDotL)
        {
            float n;

            if (UseExplicitN)
            {
                n = ExplicitN;
            }
            else
            {
                float sinGamma = (float) Math.Sqrt(1.0d - eDotL * eDotL);
                var zN = EyeNearPlaneDistance / sinGamma;

                if (UseNewNFormula)
                {
                    var z0 = -zN;
                    var z1 = -(zN + d * sinGamma);
                    n = d / ((float) Math.Sqrt(z1 / z0) - 1.0f);
                }
                else
                {
                    var zF = zN + d * sinGamma;
                    n = (zN + (float) Math.Sqrt(zF * zN)) / sinGamma;
                }
            }

            return n;
        }

        void CreateYPerspective(float n, float f, out Matrix result)
        {
            // Y 方向での n と f による透視変換。
            //
            // a = (f + n) / (f - n)
            // b = -2 * f * n / (f - n)
            //
            // 論文における行列 (GL):
            //
            // [ 1 0 0 0]
            // [ 0 a 0 b]
            // [ 0 0 1 0]
            // [ 0 1 0 0]
            //
            // ここでの行列 (DirectX & XNA):
            //
            // [ 1 0 0 0]
            // [ 0 a 0 1]
            // [ 0 0 1 0]
            // [ 0 b 0 0]
            //
            // a と b の位置の差異は、ベクトルと行列の演算順の差異による。
            // この透視射影は、X と Z についてスケールおよび移動が無い状態。
            //
            // なお、a と b を下式とする人もいる。
            //
            // a = f / (f - n)
            // b = -1 * f * n / (f - n)
            //
            // 恐らく、DirectX (XNA) の透視射影行列を意識した式と思われる
            // ([-1, 1] ではなく [0, 1] への射影)。
            // しかし、最終的な正射影行列によりスケールと位置が変換されるため、
            // どちらでも良いと思われる。

            result = Matrix.Identity;
            result.M22 = (f + n) / (f - n);
            result.M24 = 1;
            result.M42 = -2.0f * f * n / (f - n);
            result.M44 = 0;
        }

        void TransformLightVolumePoints(ref Matrix matrix)
        {
            int count = lightVolumePoints.Count;

            for (int i = 0; i < count; i++)
            {
                var source = lightVolumePoints[i];

                Vector3 destination;
                Vector3.TransformCoordinate(ref source, ref matrix, out destination);

                transformedLightVolumePoints.Add(destination);
            }
        }
    }
}
