#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    public class LiSPSMCamera : FocusedLightCamera
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

        public bool UseLiSPSM { get; set; }

        public Vector3 EyePosition;

        public Vector3 EyeDirection;

        public float EyeNearPlaneDistance;

        public LiSPSMCamera()
        {
            UseNewNFormula = true;
            UseExplicitN = false;
            ExplicitN = 10.0f;
            UseLiSPSM = true;
        }

        public override void Update()
        {
            if (!UseLiSPSM)
            {
                base.Update();
                return;
            }

            // カメラとライトのなす角の算出。
            var L = LightDirection;
            L.Normalize();

            var E = EyeDirection;
            E.Normalize();

            float eDotL;
            Vector3.Dot(ref E, ref L, out eDotL);
            if (1.0f - 0.01f <= Math.Abs(eDotL))
            {
                // カメラとライトがほぼ平行ならば外積が縮退するため、
                // USM で行列を算出。
                base.Update();
                return;
            }

            // ライトの UP ベクトル。
            Vector3 up;
            CalculateLightUp(ref L, ref E, out up);

            // 仮ライト ビュー行列。
            // オリジナルではライト位置を E、ライト方向を E + L としているが、
            // 仮ライト ビュー行列は n と f の算出のための AABB を作るためだけの変換であるため、
            // 位置は重要ではないと思われる。
            var tempLightPosition = Vector3.Zero;
            var tempLightTarget = L;
            Matrix tempLightView;
            Matrix.CreateLookAt(ref tempLightPosition, ref tempLightTarget, ref up, out tempLightView);

            // 仮ライト空間における凸体 B の AABB。
            BoundingBox lightConvexBodyBBox;
            CreateTransformedConvexBodyBBox(ref tempLightView, out lightConvexBodyBBox);

            // 錐台 P の d (n から f の距離)。
            float d = lightConvexBodyBBox.Max.Y - lightConvexBodyBBox.Min.Y;

            // 錐台 P の n (近平面)。
            float n = CalculateN(d, eDotL);

            // 錐台 P の f (遠平面)。
            float f = n + d;

            // 錐台 P のビュー行列。
            var pPosition = EyePosition - up * (n - EyeNearPlaneDistance);
            var pTarget = pPosition + L;
            Matrix.CreateLookAt(ref pPosition, ref pTarget, ref up, out LightView);

            // Y 方向での n と f による透視変換。
            Matrix lispProjection;
            CreateYPerspective(n, f, out lispProjection);

            // 錐台 P の Y 射影変換。
            Matrix pViewLispProjection;
            Matrix.Multiply(ref LightView, ref lispProjection, out pViewLispProjection);

            // 錐台 P の Y 射影空間における凸体 B の AABB。
            CreateTransformedConvexBodyBBox(ref pViewLispProjection, out lightConvexBodyBBox);

            // 正射影。
            //
            // オリジナル コードは (-1,-1,-1) から (1,1,1) の範囲へスケール変更した後、
            // 右手系座標から左手系座標へ変換 (クリッピング空間は左手系) しているが、
            // ここでは CreateOrthographicOffCenter の呼び出しでそれらを纏めている。
            // もし、オリジナル コードの手順をそのまま残しつつ DirectX に適合させたいなら、
            // オリジナル同様のスケール変更と座標系変換の後、
            // CreateOrthographicOffCenter(-1,1,-1,1,-1,1) により
            // z を (-1, 0) へスケール変更すれば良いと思われる。
            Matrix orthoProjection;
            Matrix.CreateOrthographicOffCenter(
                lightConvexBodyBBox.Min.X, lightConvexBodyBBox.Max.X,
                lightConvexBodyBBox.Min.Y, lightConvexBodyBBox.Max.Y,
                -lightConvexBodyBBox.Max.Z, -lightConvexBodyBBox.Min.Z,
                out orthoProjection);

            // 最終的な射影行列。
            Matrix.Multiply(ref lispProjection, ref orthoProjection, out LightProjection);
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
            // ([-1, 1] ではなく [-1, 0] への射影)。
            // しかし、最終的な正射影行列によりスケールと位置が変換されるため、
            // どちらでも良いと思われる。

            result = Matrix.Identity;
            result.M22 = (f + n) / (f - n);
            result.M24 = 1;
            result.M42 = -2.0f * f * n / (f - n);
            result.M44 = 0;
        }
    }
}
