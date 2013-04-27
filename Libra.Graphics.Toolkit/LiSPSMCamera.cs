#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class LiSPSMCamera : FocusedLightCamera
    {
        float adjustNFactorTweak;

        public float EyeDotLightThreshold { get; set; }

        public float AdjustNFactor { get; set; }

        /// <summary>
        /// 明示した N 値を使用するか否かを示す値を取得または設定します。
        /// true (明示した N 値を使用する場合)、false (それ以外の場合)。
        /// </summary>
        public bool UseExplicitN { get; set; }

        /// <summary>
        /// 明示する N 値を取得または設定します。
        /// </summary>
        public float ExplicitN { get; set; }

        public LiSPSMCamera()
        {
            adjustNFactorTweak = 1.0f;
            EyeDotLightThreshold = 0.9f;
            AdjustNFactor = 0.1f;
        }

        protected override void Update()
        {
            // 標準的なライト空間行列の算出。
            CalculateStandardLightSpaceMatrices();

            // 凸体 B の算出。
            CalculateBodyB();

            // 凸体 B が空の場合は生成する影が無いため、
            // 算出された行列をそのまま利用。
            if (bodyBPoints.Count == 0)
            {
                return;
            }

            // 凸体 LVS の算出。
            CalculateBodyLVS();

            CalculateAdjustNFactorTweak();

            Matrix lightSpace;
            Matrix transform;

            // 軸の変換。
            transform = NormalToLightSpace;
            TransformLightProjection(ref transform);

            // ライト空間におけるカメラ方向へ変換。
            CreateCurrentLightSpace(out lightSpace);
            CreateLightLook(ref lightSpace, out transform);
            TransformLightProjection(ref transform);

            // LiSPSM 射影。
            CreateCurrentLightSpace(out lightSpace);
            CreateLiSPSMProjection(ref lightSpace, out transform);
            TransformLightProjection(ref transform);

            // 単位立方体へ射影。
            CreateCurrentLightSpace(out lightSpace);
            CreateTransformToUnitCube(ref lightSpace, out transform);
            TransformLightProjection(ref transform);

            // 軸の変換 (元へ戻す)。
            transform = LightSpaceToNormal;
            TransformLightProjection(ref transform);

            // DirectX クリッピング空間へ変換。
            Matrix.CreateOrthographicOffCenter(-1, 1, -1, 1, -1, 1, out transform);
            TransformLightProjection(ref transform);
        }

        void CalculateAdjustNFactorTweak()
        {
            float dot;
            Vector3.Dot(ref eyeDirection, ref lightDirection, out dot);

            if (EyeDotLightThreshold <= dot)
            {
                adjustNFactorTweak = 1.0f + 20.0f * (dot - EyeDotLightThreshold) / (1.0f - EyeDotLightThreshold);
            }
            else
            {
                adjustNFactorTweak = 1.0f;
            }
        }

        void CreateLiSPSMProjection(ref Matrix lightSpace, out Matrix result)
        {
            // 凸体 B のライト空間における AABB。
            BoundingBox bodyBBoxLS;
            CreateTransformedBodyBBox(ref lightSpace, out bodyBBoxLS);

            // 錐台 P の n (近平面)。
            var n = CalculateN(ref lightSpace, ref bodyBBoxLS);
            if (n <= 0.0f)
            {
                result = Matrix.Identity;
                return;
            }

            // 錐台 P の d (近平面から遠平面までの距離)。
            var d = Math.Abs(bodyBBoxLS.Max.Z - bodyBBoxLS.Min.Z);

            // TODO
            // CalculateNGeneral を呼び出すルートの場合、二度同じ計算をしている。
            Vector3 cameraPointWS;
            GetNearCameraPointWS(out cameraPointWS);

            Vector3 cameraPointLS;
            Vector3.TransformCoordinate(ref cameraPointWS, ref lightSpace, out cameraPointLS);

            // 錐台 P の視点位置。
            var pPositionBase = new Vector3(cameraPointLS.X, cameraPointLS.Y, bodyBBoxLS.Max.Z);
            var pPosition = pPositionBase + n * Vector3.Backward;

            // 錐台 P の視点位置への移動行列。
            var pTranslation = Matrix.CreateTranslation(-pPosition);

            // 錐台 P の透視射影。
            Matrix pPerspective;
            CreatePerspective(-1, 1, -1, 1, n + d, n, out pPerspective);

            // 最終的な LiSPSM 射影行列。
            Matrix.Multiply(ref pTranslation, ref pPerspective, out result);
        }

        float CalculateN(ref Matrix lightSpace, ref BoundingBox bodyBBoxLS)
        {
            if (UseExplicitN)
            {
                return ExplicitN;
            }

            return CalculateNGeneral(ref lightSpace, ref bodyBBoxLS);
            //return CalculateNSimple(ref lightSpace, ref bodyBBoxLS);
        }

        float CalculateNGeneral(ref Matrix lightSpace, ref BoundingBox bodyBBoxLS)
        {
            Matrix inverseLightSpace;
            Matrix.Invert(ref lightSpace, out inverseLightSpace);

            Vector3 cameraPointWS;
            GetNearCameraPointWS(out cameraPointWS);

            // z0 と z1 の算出。

            // ライト空間。
            Vector3 z0LS;
            Vector3 z1LS;
            CalculateZ0LS(ref lightSpace, ref cameraPointWS, ref bodyBBoxLS, out z0LS);
            z1LS = new Vector3(z0LS.X, z0LS.Y, bodyBBoxLS.Min.Z);

            // ワールド空間。
            Vector3 z0WS;
            Vector3 z1WS;
            Vector3.TransformCoordinate(ref z0LS, ref inverseLightSpace, out z0WS);
            Vector3.TransformCoordinate(ref z1LS, ref inverseLightSpace, out z1WS);

            // 表示カメラ空間。
            Vector3 z0ES;
            Vector3 z1ES;
            Vector3.TransformCoordinate(ref z0WS, ref eyeView, out z0ES);
            Vector3.TransformCoordinate(ref z1WS, ref eyeView, out z1ES);

            var z0 = z0ES.Z;
            var z1 = z1ES.Z;

            // TODO
            //
            // Ogre の式では精度が異常に劣化する。
            // Ogre の式は、古い式の factor をパラメータ化した物、
            // オリジナルは新しい式で factor 無し、であると思われる。
            // 精度の劣化は、恐らく、デフォルトの AdjustNFactor が不適切であり、
            // 適切な値を明示する必要があると考えられる。

            // オリジナルの場合。
            //float d = Math.Abs(bodyBBoxLS.Max.Z - bodyBBoxLS.Min.Z);
            //return d / ((float) Math.Sqrt(z1 / z0) - 1.0f);

            // Ogre の場合。
            if ((z0 < 0 && 0 < z1) || (z1 < 0 && 0 < z0))
                return 0.0f;

            return EyeNearDistance + (float) Math.Sqrt(z0 * z1) * AdjustNFactor * adjustNFactorTweak;
        }

        float CalculateNSimple(ref Matrix lightSpace, ref BoundingBox bodyBBoxLS)
        {
            // TODO
            // Ogre の calculateNOptSimple。
            // 古い式の、ライト空間での演算を行わないバージョンか？
            Vector3 cameraPointWS;
            GetNearCameraPointWS(out cameraPointWS);

            Vector3 cameraPointES;
            Vector3.TransformCoordinate(ref cameraPointWS, ref eyeView, out cameraPointES);

            return (Math.Abs(cameraPointES.Z) + (float) Math.Sqrt(EyeNearDistance * EyeFarDistance)) * AdjustNFactor * adjustNFactorTweak;
        }

        void CalculateZ0LS(ref Matrix lightSpace, ref Vector3 cameraWS, ref BoundingBox bodyBBoxLS, out Vector3 result)
        {
            var plane = new Plane(eyeDirection, cameraWS);
            Plane.Transform(ref plane, ref lightSpace, out plane);

            Vector3 cameraLS;
            Vector3.TransformCoordinate(ref cameraWS, ref lightSpace, out cameraLS);

            // TODO
            //
            // オリジナルのままでは、ライトのある方向へカメラを向けた場合に、
            // 正常に描画されなくなる。

            // オリジナルの場合。
            // オリジナルの Plane は D の符号が逆。
            //result.X = cameraLS.X;
            //result.Y = -plane.D - (plane.Normal.Z * bodyBBoxLS.Max.Z - plane.Normal.X * cameraLS.X) / plane.Normal.Y;
            //result.Z = bodyBBoxLS.Max.Z;

            // Ogre の場合。
            var ray = new Ray(new Vector3(cameraLS.X, 0.0f, bodyBBoxLS.Max.Z), Vector3.UnitY);
            var intersect = ray.Intersects(plane);

            if (intersect != null)
            {
                ray.GetPoint(intersect.Value, out result);
            }
            else
            {
                ray.Direction = Vector3.NegativeUnitY;
                intersect = ray.Intersects(plane);

                if (intersect != null)
                {
                    ray.GetPoint(intersect.Value, out result);
                }
                else
                {
                    result = Vector3.Zero;
                }
            }
        }

        void CreatePerspective(float left, float right, float bottom, float top, float near, float far, out Matrix result)
        {
            // 即ち、glFrustum。XNA CreatePerspectiveOffCenter に相当。
            // http://msdn.microsoft.com/ja-jp/library/windows/desktop/dd373537(v=vs.85).aspx
            // z 軸の範囲が OpenGL では (-1, 1)、XNA/DirectX では (-1, 0)。

            result = new Matrix();

            result.M11 = 2.0f * near / (right - left);
            result.M22 = 2.0f * near / (top - bottom);
            result.M31 = (right + left) / (right - left);
            result.M32 = (top + bottom) / (top - bottom);
            result.M33 = -(far + near) / (far - near);
            result.M34 = -1.0f;
            result.M43 = -2.0f * far * near / (far - near);
        }
    }
}
