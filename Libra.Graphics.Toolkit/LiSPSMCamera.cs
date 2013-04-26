#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class LiSPSMCamera : FocusedLightCamera
    {
        float adjustNFactorTweak;

        public float EyeNearPlaneDistance { get; set; }

        public float EyeLightDirectionThreshold { get; set; }

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
            EyeLightDirectionThreshold = 0.9f;
            AdjustNFactor = 0.1f;
        }

        protected override void Update()
        {
            // 標準的なライト空間行列の算出。
            CalculateStandardLightSpaceMatrices();

            // 凸体 B が空の場合は生成する影が無いため、
            // 算出された行列をそのまま利用。
            if (ConvexBodyBPoints.Count == 0)
            {
                return;
            }

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
            float eDotL;
            Vector3.Dot(ref eyeDirection, ref lightDirection, out eDotL);

            if (EyeLightDirectionThreshold <= eDotL)
            {
                adjustNFactorTweak = 1.0f + 20.0f * (eDotL - EyeLightDirectionThreshold) / (1.0f - EyeLightDirectionThreshold);
            }
            else
            {
                adjustNFactorTweak = 1.0f;
            }
        }

        void CreateLiSPSMProjection(ref Matrix lightSpace, out Matrix result)
        {
            BoundingBox bodyBBoxLS;
            CreateTransformedConvexBodyBBox(ref lightSpace, out bodyBBoxLS);

            // 錐台 P の n (近平面)。
            var n = CalculateN(ref lightSpace, ref bodyBBoxLS);
            if (n <= 0.0f)
            {
                result = Matrix.Identity;
                return;
            }

            // 錐台 P の d (近平面から遠平面までの距離)。
            var d = Math.Abs(bodyBBoxLS.Max.Z - bodyBBoxLS.Min.Z);

            Vector3 cameraPointWS;
            GetNearCameraPointWS(out cameraPointWS);

            Vector3 cameraPointLS;
            Vector3.TransformCoordinate(ref cameraPointWS, ref lightSpace, out cameraPointLS);

            // 錐台 P の視点位置。
            var pPositionBase = new Vector3(cameraPointLS.X, cameraPointLS.Y, bodyBBoxLS.Max.Z);
            var pPosition = pPositionBase + n * Vector3.UnitZ;

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
        }

        float CalculateNGeneral(ref Matrix lightSpace, ref BoundingBox bodyBBoxLS)
        {
            Matrix inverseLightSpace;
            Matrix.Invert(ref lightSpace, out inverseLightSpace);

            Vector3 cameraWS;
            GetNearCameraPointWS(out cameraWS);

            // z0 と z1 の算出。

            // ライト空間。
            Vector3 z0LS;
            Vector3 z1LS;
            CalculateZ0LS(ref lightSpace, ref cameraWS, ref bodyBBoxLS, out z0LS);
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

            if ((z0 < 0 && 0 < z1) || (z1 < 0 && 0 < z0))
                return 0.0f;

            return EyeNearPlaneDistance + (float) Math.Sqrt(z0 * z1) * AdjustNFactor * adjustNFactorTweak;
        }

        void CalculateZ0LS(ref Matrix lightSpace, ref Vector3 cameraWS, ref BoundingBox bodyBBoxLS, out Vector3 result)
        {
            var plane = new Plane(eyeDirection, cameraWS);
            Plane.Transform(ref plane, ref lightSpace, out plane);

            Vector3 cameraLS;
            Vector3.TransformCoordinate(ref cameraWS, ref lightSpace, out cameraLS);

            // TODO
            //
            // 以下、誤り。
            // そもそも、ここは LVS 凸体 B を算出して用いる場面であるため、
            // クラス外部で押し出した 凸体 B を参照していることが誤り。
            //
            // 凸体 B のライト方向への押し出し方によって大きく挙動が変化してしまう。
            // Ogre は Ogre 版の押し出しでなければ Z0LS がおかしくなる。
            // オリジナルは Ogre 版でもそれなりだが、オリジナルとの相性が良い。

            // オリジナルの場合。
            result.X = cameraLS.X;
            result.Y = plane.D - (plane.Normal.Z * bodyBBoxLS.Max.Z - plane.Normal.X * cameraLS.X) / plane.Normal.Y;
            result.Z = bodyBBoxLS.Max.Z;

            // Ogre の場合。
            //var ray = new Ray(new Vector3(cameraLS.X, 0.0f, bodyBBoxLS.Max.Z), Vector3.UnitY);
            //var intersect = ray.Intersects(plane);

            //if (intersect != null)
            //{
            //    ray.GetPoint(intersect.Value, out result);
            //}
            //else
            //{
            //    ray.Direction = -Vector3.UnitY;
            //    intersect = ray.Intersects(plane);

            //    if (intersect != null)
            //    {
            //        ray.GetPoint(intersect.Value, out result);
            //    }
            //    else
            //    {
            //        result = Vector3.Zero;
            //    }
            //}
        }

        void CreatePerspective(float left, float right, float bottom, float top, float near, float far, out Matrix result)
        {
            // 即ち、glFrustum。XNA CreatePerspectiveOffCenter に相当。
            // http://msdn.microsoft.com/ja-jp/library/windows/desktop/dd373537(v=vs.85).aspx
            // z 軸の範囲が OpenGL では (-1, 1)、XNA/DirectX では (-1, 0)。
            // ただし、右手系から左手系への変換を省くために z の符号を反転。
            // このため、呼び出し側での near と far の指定に注意 (これも逆転が必要)。

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
