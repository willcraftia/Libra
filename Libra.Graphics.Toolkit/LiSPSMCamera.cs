#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class LiSPSMCamera : FocusedLightCamera
    {
        public float EyeNearPlaneDistance;

        float adjustFactorTweak;

        public float EyeLightDirectionThreshold { get; set; }

        public float AdjustFactor { get; set; }

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
            adjustFactorTweak = 1.0f;
            EyeLightDirectionThreshold = 0.9f;
            AdjustFactor = 0.1f;
        }

        public override void Update()
        {
            // 標準的なライト空間行列の算出。
            CalculateLightSpace();

            // 凸体 B が空の場合は生成する影が無いため、
            // 算出された行列をそのまま利用。
            if (ConvexBodyBPoints.Count == 0)
            {
                return;
            }

            float eDotL;
            Vector3.Dot(ref eyeDirection, ref lightDirection, out eDotL);
            if (EyeLightDirectionThreshold <= eDotL)
            {
                adjustFactorTweak = 1.0f + 20.0f * (eDotL - EyeLightDirectionThreshold) / (1.0f - EyeLightDirectionThreshold);
            }
            else
            {
                adjustFactorTweak = 1.0f;
            }

            // 軸の変換。
            // y -> -z
            // z -> y
            LightProjection = LightProjection * NormalToLightSpace;

            // ライト空間におけるカメラ方向を算出。
            var projViewDir = getProjViewDir_ls(LightView * LightProjection);
            // そのカメラ方向が示すビュー空間へライト空間を変換。
            LightProjection = LightProjection * Matrix.CreateLook(Vector3.Zero, projViewDir, Vector3.UnitY);

            // LiSPSM 射影。
            LightProjection = LightProjection * CalculateLiSPSM(LightView * LightProjection);

            // 単位立方体へ射影。
            LightProjection = LightProjection * TransformToUnitCube(LightView * LightProjection);

            // 軸の変換 (元へ戻す)。
            LightProjection = LightProjection * LightSpaceToNormal;

            // DirectX クリッピング空間へ変換。
            LightProjection = LightProjection * Matrix.CreateOrthographicOffCenter(-1, 1, -1, 1, -1, 1);
        }

        Matrix CalculateLiSPSM(Matrix lightSpace)
        {
            BoundingBox bodyBBox_ls;
            CreateTransformedConvexBodyBBox(ref lightSpace, out bodyBBox_ls);

            var n = CalculateN(lightSpace, bodyBBox_ls);
            if (n <= 0.0f)
            {
                return Matrix.Identity;
            }

            var e_ls = Vector3.TransformCoordinate(getNearEyePositionWorld(), lightSpace);

            var C_start_ls = new Vector3(e_ls.X, e_ls.Y, bodyBBox_ls.Max.Z);

            var C = C_start_ls + n * Vector3.UnitZ;

            var lightSpaceTranslation = Matrix.CreateTranslation(-C);

            var d = Math.Abs(bodyBBox_ls.Max.Z - bodyBBox_ls.Min.Z);

            Matrix P = BuildFrustumProjection(-1, 1, -1, 1, n + d, n);

            return lightSpaceTranslation * P;
        }

        float CalculateN(Matrix lightSpace, BoundingBox bodyBBox_ls)
        {
            if (UseExplicitN)
            {
                return ExplicitN;
            }

            return CalculateNGeneral(lightSpace, bodyBBox_ls);
        }

        float CalculateNGeneral(Matrix lightSpace, BoundingBox bodyBBox_ls)
        {
            Matrix inverseLightSpace;
            Matrix.Invert(ref lightSpace, out inverseLightSpace);

            var e_ws = getNearEyePositionWorld();
            
            var z0_ls = CalculateZ0_ls(lightSpace, e_ws, bodyBBox_ls.Max.Z);
            var z1_ls = new Vector3(z0_ls.X, z0_ls.Y, bodyBBox_ls.Min.Z);

            var z0_ws = Vector3.TransformCoordinate(z0_ls, inverseLightSpace);
            var z1_ws = Vector3.TransformCoordinate(z1_ls, inverseLightSpace);

            var z0_es = Vector3.TransformCoordinate(z0_ws, eyeView);
            var z1_es = Vector3.TransformCoordinate(z1_ws, eyeView);

            var z0 = z0_es.Z;
            var z1 = z1_es.Z;
            var d = Math.Abs(bodyBBox_ls.Max.Z - bodyBBox_ls.Min.Z);

            // TODO
            // 一応、ゼロ除算の回避が必要なのでは？
            //return d / ((float) Math.Sqrt(z1 / z0) - 1.0f);

            if ((z0 < 0 && 0 < z1) || (z1 < 0 && 0 < z0))
                return 0.0f;

            return EyeNearPlaneDistance + (float) Math.Sqrt(z0 * z1) * AdjustFactor * adjustFactorTweak;
        }

        Vector3 CalculateZ0_ls(Matrix lightSpace, Vector3 e, float bodyB_zMax_ls)
        {
            var plane = new Plane(eyeDirection, e);
            plane = Plane.Transform(plane, lightSpace);

            var e_ls = Vector3.TransformCoordinate(e, lightSpace);
            var ray = new Ray(new Vector3(e_ls.X, 0.0f, bodyB_zMax_ls), Vector3.UnitY);
            var intersect = ray.Intersects(plane);

            if (intersect != null)
            {
                return ray.GetPoint(intersect.Value);
            }
            else
            {
                ray = new Ray(new Vector3(e_ls.X, 0.0f, bodyB_zMax_ls), -Vector3.UnitY);
                intersect = ray.Intersects(plane);

                if (intersect != null)
                {
                    return ray.GetPoint(intersect.Value);
                }
                else
                {
                    return Vector3.Zero;
                }
            }
        }

        Matrix BuildFrustumProjection(float left, float right, float bottom, float top, float near, float far)
        {
            // 即ち、glFrustum。XNA CreatePerspectiveOffCenter に相当。
            // http://msdn.microsoft.com/ja-jp/library/windows/desktop/dd373537(v=vs.85).aspx
            // z 軸の範囲が OpenGL では (-1, 1)、XNA/DirectX では (-1, 0)。
            // ただし、右手系から左手系への変換を省くために z の符号を反転。
            // このため、呼び出し側での near と far の指定に注意 (これも逆転が必要)。

            var result = new Matrix();

            // TODO
            // 行列の演算順を逆転させているので、
            // オリジナルや Ogre の転置としているが、あっているか？

            result.M11 = 2.0f * near / (right - left);
            result.M22 = 2.0f * near / (top - bottom);
            result.M31 = (right + left) / (right - left);
            result.M32 = (top + bottom) / (top - bottom);
            result.M33 = -(far + near) / (far - near);
            result.M34 = -1.0f;
            result.M43 = -2.0f * far * near / (far - near);

            return result;
        }
    }
}
