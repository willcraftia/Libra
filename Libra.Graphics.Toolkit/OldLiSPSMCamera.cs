#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    public class OldLiSPSMCamera : FocusedLightCamera
    {
        /// <summary>
        /// 新しい N 算出式を使用するか否かを示す値を取得または設定します。
        /// true (新しい N 算出式を使用する場合)、false (それ以外の場合)。
        /// </summary>
        public bool UseNewNFormula { get; set; }

        /// <summary>
        /// 明示した N 値を使用するか否かを示す値を取得または設定します。
        /// true (明示した N 値を使用する場合)、false (それ以外の場合)。
        /// </summary>
        public bool UseExplicitN { get; set; }

        /// <summary>
        /// 明示する N 値を取得または設定します。
        /// </summary>
        public float ExplicitN { get; set; }

        public OldLiSPSMCamera()
        {
            UseNewNFormula = true;
            UseExplicitN = false;
            ExplicitN = 10.0f;
        }

        protected override void Update()
        {
            View = Matrix.Identity;
            Projection = Matrix.Identity;

            CalculateBodyB();

            // カメラとライトのなす角。
            float eDotL;
            Vector3.Dot(ref eyeDirection, ref lightDirection, out eDotL);

            // UP ベクトル。
            Vector3 up;
            CalculateUp(ref lightDirection, ref eyeDirection, out up);

            // 仮ビュー行列。
            Matrix.CreateLook(ref eyePosition, ref lightDirection, ref up, out View);

            // 仮ビュー空間における凸体 B の AABB。
            BoundingBox bodyBBox;
            CreateTransformedBodyBBox(ref View, out bodyBBox);

            // n から f の距離。
            float d = bodyBBox.Max.Y - bodyBBox.Min.Y;

            // 近平面。
            float n = CalculateN(d, eDotL);
            if (n <= 0.0f)
            {
                base.Update();
                return;
            }

            // 遠平面。
            float f = n + d;

            // ビュー行列。
            var pPosition = eyePosition - up * (n - eyeProjectionNear);
            Matrix.CreateLook(ref pPosition, ref lightDirection, ref up, out View);

            Matrix lightSpace;
            Matrix transform;

            // Y 方向での n と f による透視変換。
            CreateYPerspective(n, f, out transform);
            TransformLightProjection(ref transform);

            // Y 射影変換。
            CreateCurrentLightSpace(out lightSpace);
            CreateTransformToUnitCube(ref lightSpace, out transform);
            TransformLightProjection(ref transform);

            // DirectX クリッピング空間へ変換。
            Matrix.CreateOrthographicOffCenter(-1, 1, -1, 1, -1, 1, out transform);
            TransformLightProjection(ref transform);
        }

        void CalculateUp(ref Vector3 L, ref Vector3 E, out Vector3 result)
        {
            Vector3 left;
            Vector3.Cross(ref L, ref E, out left);

            Vector3.Cross(ref left, ref L, out result);
            result.Normalize();
        }

        float CalculateN(float d, float eDotL)
        {
            if (UseExplicitN)
            {
                return ExplicitN;
            }

            if ((1 - MathHelper.ZeroTolerance) < Math.Abs(eDotL))
            {
                // eDotL = 1 で sinGamma = 0。
                // eDotL = 1 とは、即ち、E と L が並行。
                // また、sinGamma = 0 はゼロ除算を発生させる問題もある。
                // sinGamma が概ねゼロの場合、n = 0 とし、歪み無しで扱う。
                return 0.0f;
            }

            float sinGamma = (float) Math.Sqrt(1.0 - eDotL * eDotL);

            var zN = eyeProjectionNear / sinGamma;

            if (UseNewNFormula)
            {
                var z0 = -zN;
                var z1 = -(zN + d * sinGamma);
                return d / ((float) Math.Sqrt(z1 / z0) - 1.0f);
            }
            else
            {
                var zF = zN + d * sinGamma;
                return (zN + (float) Math.Sqrt(zF * zN)) / sinGamma;
            }
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
            // a と b の位置の差異は、ベクトルと行列の積の方法による。
            // この透視射影は、X と Z についてスケールおよび移動が無い状態。
            //
            // なお、a と b を下式とする人もいる。
            //
            // a = f / (f - n)
            // b = -1 * f * n / (f - n)
            //
            // 恐らく、DirectX (XNA) の透視射影行列を意識した式と思われる
            // ([-1, 1] ではなく [-1, 0] への射影)。
            // しかし、クリッピングを行う箇所はここではないと思われる。

            result = Matrix.Identity;
            result.M22 = (f + n) / (f - n);
            result.M24 = 1;
            result.M42 = -2.0f * f * n / (f - n);
            result.M44 = 0;
        }
    }
}
