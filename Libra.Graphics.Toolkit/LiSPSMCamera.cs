#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class LiSPSMCamera : FocusedLightCamera
    {
        static readonly Matrix NormalToLightSpace = new Matrix(
            1, 0, 0, 0,
            0, 0, 1, 0,
            0, -1, 0, 0,
            0, 0, 0, 1);

        static readonly Matrix LightSpaceToNormal = new Matrix(
            1, 0, 0, 0,
            0, 0, -1, 0,
            0, 1, 0, 0,
            0, 0, 0, 1);

        public Matrix EyeView;

        public float EyeNearPlaneDistance;

        Vector3 eyePosition;

        Vector3 eyeDirection;

        Vector3 eyeUp;

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
            EyeView = Matrix.Identity;
            adjustFactorTweak = 1.0f;
            EyeLightDirectionThreshold = 0.9f;
            AdjustFactor = 0.1f;
        }

        public override void Update()
        {
            Matrix inverseEyeView;
            Matrix.Invert(ref EyeView, out inverseEyeView);

            eyePosition = inverseEyeView.Translation;
            eyeDirection = inverseEyeView.Forward;
            eyeUp = inverseEyeView.Up;

            // 標準的なライト空間行列の算出。
            CalculateLightSpace();

            // 凸体 B が空の場合は生成する影が無いため、
            // 算出された行列をそのまま利用。
            if (ConvexBodyBPoints.Count == 0)
            {
                return;
            }

            float eDotL;
            Vector3.Dot(ref eyeDirection, ref LightDirection, out eDotL);
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

            LightProjection = LightProjection * CalculateLiSPSM(LightView * LightProjection);

            LightProjection = LightProjection * TransformToUnitCube(LightView * LightProjection);

            LightProjection = LightProjection * LightSpaceToNormal;

            // for DirectX clipping space
            LightProjection = LightProjection * Matrix.CreateOrthographicOffCenter(-1, 1, -1, 1, -1, 1);
        }

        void CalculateLightSpace()
        {
            // 方向性光源のための行列。
            Matrix.CreateLook(ref eyePosition, ref LightDirection, ref eyeDirection, out LightView);
            LightProjection = Matrix.Identity;

            // TODO: 点光源
        }

        Vector3 getProjViewDir_ls(Matrix lightSpace)
        {
            // 
            var e = getNearCameraPoint_ws();
            var b = e + eyeDirection;

            // ライト空間へ変換。
            var e_ls = Vector3.TransformCoordinate(e, lightSpace);
            var b_ls = Vector3.TransformCoordinate(b, lightSpace);

            var projDir = b_ls - e_ls;
            projDir.Y = 0.0f;

            return projDir;
        }

        Vector3 getNearCameraPoint_ws()
        {
            if (ConvexBodyBPoints.Count == 0)
                return Vector3.Zero;

            var nearWorld = ConvexBodyBPoints[0];
            var nearEye = Vector3.TransformCoordinate(nearWorld, EyeView);

            for (int i = 1; i < ConvexBodyBPoints.Count; i++)
            {
                var world = ConvexBodyBPoints[i];
                var eye = Vector3.TransformCoordinate(world, EyeView);

                if (nearEye.Z < eye.Z)
                {
                    nearEye = eye;
                    nearWorld = world;
                }
            }

            return nearWorld;
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

            var e_ls = Vector3.TransformCoordinate(getNearCameraPoint_ws(), lightSpace);

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

            var e_ws = getNearCameraPoint_ws();
            
            var z0_ls = CalculateZ0_ls(lightSpace, e_ws, bodyBBox_ls.Max.Z);
            var z1_ls = new Vector3(z0_ls.X, z0_ls.Y, bodyBBox_ls.Min.Z);

            var z0_ws = Vector3.TransformCoordinate(z0_ls, inverseLightSpace);
            var z1_ws = Vector3.TransformCoordinate(z1_ls, inverseLightSpace);

            var z0_es = Vector3.TransformCoordinate(z0_ws, EyeView);
            var z1_es = Vector3.TransformCoordinate(z1_ws, EyeView);

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

        Matrix TransformToUnitCube(Matrix lightSpace)
        {
            var bodyBBox = new BoundingBox();
            CreateTransformedConvexBodyBBox(ref lightSpace, out bodyBBox);

            Matrix result;
            CreateTransformToUnitCube(ref bodyBBox.Min, ref bodyBBox.Max, out result);

            return result;
        }

        void CreateTransformToUnitCube(ref Vector3 min, ref Vector3 max, out Matrix result)
        {
            // 即ち glOrtho と等価。
            // http://msdn.microsoft.com/en-us/library/windows/desktop/dd373965(v=vs.85).aspx
            // ただし、右手系から左手系への変換を省くために z スケールの符号を反転。

            result = new Matrix();

            result.M11 = 2.0f / (max.X - min.X);
            result.M22 = 2.0f / (max.Y - min.Y);
            result.M33 = 2.0f / (max.Z - min.Z);
            result.M41 = -(max.X + min.X) / (max.X - min.X);
            result.M42 = -(max.Y + min.Y) / (max.Y - min.Y);
            result.M43 = -(max.Z + min.Z) / (max.Z - min.Z);
            result.M44 = 1.0f;
        }
    }
}
