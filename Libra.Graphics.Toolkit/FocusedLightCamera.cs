#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    public class FocusedLightCamera : LightCamera
    {
        // y -> -z
        // z -> y
        protected static readonly Matrix NormalToLightSpace = new Matrix(
            1,  0,  0,  0,
            0,  0,  1,  0,
            0, -1,  0,  0,
            0,  0,  0,  1);

        // y -> z
        // z -> -y
        protected static readonly Matrix LightSpaceToNormal = new Matrix(
            1,  0,  0,  0,
            0,  0, -1,  0,
            0,  1,  0,  0,
            0,  0,  0,  1);

        /// <summary>
        /// 視点カメラの位置。
        /// </summary>
        protected Vector3 eyePosition;

        /// <summary>
        /// 視点カメラの方向。
        /// </summary>
        protected Vector3 eyeDirection;

        /// <summary>
        /// 視点カメラの UP ベクトル。
        /// </summary>
        protected Vector3 eyeUp;

        /// <summary>
        /// 視点カメラのビュー行列。
        /// </summary>
        protected Matrix eyeView;

        // ライトの方向 (進行方向)
        protected Vector3 lightDirection;

        /// <summary>
        /// 視点カメラのビュー行列を取得または設定します。
        /// </summary>
        /// <remarks>
        /// ビュー行列の設定では、その逆行列から視点カメラ位置、方向、UP ベクトルが抽出されます。
        /// </remarks>
        public Matrix EyeView
        {
            get { return eyeView; }
            set
            {
                eyeView = value;

                Matrix inverseEyeView;
                Matrix.Invert(ref eyeView, out inverseEyeView);

                eyePosition = inverseEyeView.Translation;
                eyeDirection = inverseEyeView.Forward;
                eyeUp = inverseEyeView.Up;
            }
        }

        /// <summary>
        /// ライトの進行方向を取得または設定します。
        /// </summary>
        public Vector3 LightDirection
        {
            get { return lightDirection; }
            set { lightDirection = value; }
        }

        protected List<Vector3> ConvexBodyBPoints { get; private set; }

        Vector3[] corners;

        public FocusedLightCamera()
        {
            eyeView = Matrix.Identity;
            eyePosition = Vector3.Zero;
            eyeDirection = Vector3.Forward;
            eyeUp = Vector3.Up;
            lightDirection = Vector3.Down;
            corners = new Vector3[BoundingBox.CornerCount];
            ConvexBodyBPoints = new List<Vector3>();
        }

        public void SetConvexBodyBPoints(IEnumerable<Vector3> points)
        {
            if (points == null) throw new ArgumentNullException("points");

            ConvexBodyBPoints.Clear();

            foreach (var point in points)
                ConvexBodyBPoints.Add(point);
        }

        public void SetConvexBodyB(ConvexBody convexBodyB, BoundingBox sceneBox)
        {
            if (convexBodyB == null) throw new ArgumentNullException("convexBodyB");

            ConvexBodyBPoints.Clear();

            for (int ip = 0; ip < convexBodyB.Polygons.Count; ip++)
            {
                var polygon = convexBodyB.Polygons[ip];

                for (int iv = 0; iv < polygon.Vertices.Count; iv++)
                {
                    ConvexBodyBPoints.Add(polygon.Vertices[iv]);
                }
            }

            int count = ConvexBodyBPoints.Count;
            for (int i = 0; i < count; i++)
            {
                var ray = new Ray(ConvexBodyBPoints[i], lightDirection);
                float? intersect;
                ray.Intersects(ref sceneBox, out intersect);
                if (intersect != null)
                {
                    var newPoint = ray.Position + ray.Direction * intersect.Value;
                    ConvexBodyBPoints.Add(newPoint);
                }
            }
        }

        public override void Update()
        {
            // 標準的なライト空間行列の算出。
            CalculateStandardLightSpaceMatrices();

            // 凸体 B が空の場合は生成する影が無いため、
            // 算出された行列をそのまま利用。
            if (ConvexBodyBPoints.Count == 0)
            {
                return;
            }

            Matrix lightSpace;
            Matrix transform;

            // 軸の変換。
            transform = NormalToLightSpace;
            TransformLightProjection(ref transform);

            // ライト空間におけるカメラ方向へ変換。
            CreateCurrentLightSpace(out lightSpace);
            CreateLightLook(ref lightSpace, out transform);
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

        protected void CalculateStandardLightSpaceMatrices()
        {
            // 方向性光源のための行列。
            Matrix.CreateLook(ref eyePosition, ref lightDirection, ref eyeDirection, out LightView);
            LightProjection = Matrix.Identity;

            // TODO: 点光源
        }

        protected void CreateCurrentLightSpace(out Matrix result)
        {
            Matrix.Multiply(ref LightView, ref LightProjection, out result);
        }

        protected void CreateLightLook(ref Matrix lightSpace, out Matrix result)
        {
            Vector3 lookPosition = Vector3.Zero;
            Vector3 lookUp = Vector3.Up;
            Vector3 lookDirection;

            GetCameraDirectionLS(ref lightSpace, out lookDirection);
            Matrix.CreateLook(ref lookPosition, ref lookDirection, ref lookUp, out result);
        }

        protected void GetNearCameraPointWS(out Vector3 result)
        {
            if (ConvexBodyBPoints.Count == 0)
            {
                result = Vector3.Zero;
                return;
            }

            Vector3 nearWS = ConvexBodyBPoints[0];
            Vector3 nearES;
            Vector3.TransformCoordinate(ref nearWS, ref eyeView, out nearES);

            for (int i = 1; i < ConvexBodyBPoints.Count; i++)
            {
                Vector3 pointWS = ConvexBodyBPoints[i];
                Vector3 pointES;
                Vector3.TransformCoordinate(ref pointWS, ref eyeView, out pointES);

                if (nearES.Z < pointES.Z)
                {
                    nearES = pointES;
                    nearWS = pointWS;
                }
            }

            result = nearWS;
        }

        protected void GetCameraDirectionLS(ref Matrix lightSpace, out Vector3 result)
        {
            Vector3 a;
            Vector3 b;

            GetNearCameraPointWS(out a);
            b = a + eyeDirection;

            // ライト空間へ変換。
            Vector3 aLS;
            Vector3 bLS;
            Vector3.TransformCoordinate(ref a, ref lightSpace, out aLS);
            Vector3.TransformCoordinate(ref b, ref lightSpace, out bLS);

            // 方向。
            result = bLS - aLS;
            
            // xz 平面 (シャドウ マップ) と平行に。
            result.Y = 0.0f;
        }

        protected void CreateTransformToUnitCube(ref Matrix lightSpace, out Matrix result)
        {
            BoundingBox bodyBBox;
            CreateTransformedConvexBodyBBox(ref lightSpace, out bodyBBox);

            CreateTransformToUnitCube(ref bodyBBox.Min, ref bodyBBox.Max, out result);
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

        protected void CreateTransformedConvexBodyBBox(ref Matrix matrix, out BoundingBox result)
        {
            result = new BoundingBox();
            for (int i = 0; i < ConvexBodyBPoints.Count; i++)
            {
                var point = ConvexBodyBPoints[i];

                Vector3 transformed;
                Vector3.TransformCoordinate(ref point, ref matrix, out transformed);

                result.Merge(ref transformed);
            }
        }

        protected void TransformLightProjection(ref Matrix matrix)
        {
            Matrix result;
            Matrix.Multiply(ref LightProjection, ref matrix, out result);

            LightProjection = result;
        }
    }
}
