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
            CalculateLightSpace();

            // 凸体 B が空の場合は生成する影が無いため、
            // 算出された行列をそのまま利用。
            if (ConvexBodyBPoints.Count == 0)
            {
                return;
            }

            // 軸の変換。
            LightProjection = LightProjection * NormalToLightSpace;

            // ライト空間におけるカメラ方向を算出。
            var projViewDir = getProjViewDir_ls(LightView * LightProjection);
            // そのカメラ方向が示すビュー空間へライト空間を変換。
            LightProjection = LightProjection * Matrix.CreateLook(Vector3.Zero, projViewDir, Vector3.Up);

            // 単位立方体へ射影。
            LightProjection = LightProjection * TransformToUnitCube(LightView * LightProjection);

            // 軸の変換 (元へ戻す)。
            LightProjection = LightProjection * LightSpaceToNormal;

            // DirectX クリッピング空間へ変換。
            LightProjection = LightProjection * Matrix.CreateOrthographicOffCenter(-1, 1, -1, 1, -1, 1);
        }

        protected void CalculateLightSpace()
        {
            // 方向性光源のための行列。
            Matrix.CreateLook(ref eyePosition, ref lightDirection, ref eyeDirection, out LightView);
            LightProjection = Matrix.Identity;

            // TODO: 点光源
        }

        protected Vector3 getNearEyePositionWorld()
        {
            if (ConvexBodyBPoints.Count == 0)
                return Vector3.Zero;

            var nearWorld = ConvexBodyBPoints[0];
            var nearEye = Vector3.TransformCoordinate(nearWorld, eyeView);

            for (int i = 1; i < ConvexBodyBPoints.Count; i++)
            {
                var world = ConvexBodyBPoints[i];
                var eye = Vector3.TransformCoordinate(world, eyeView);

                if (nearEye.Z < eye.Z)
                {
                    nearEye = eye;
                    nearWorld = world;
                }
            }

            return nearWorld;
        }

        protected Vector3 getProjViewDir_ls(Matrix lightSpace)
        {
            var e = getNearEyePositionWorld();
            var b = e + eyeDirection;

            // ライト空間へ変換。
            var e_ls = Vector3.TransformCoordinate(e, lightSpace);
            var b_ls = Vector3.TransformCoordinate(b, lightSpace);

            var projDir = b_ls - e_ls;
            projDir.Y = 0.0f;

            return projDir;
        }

        protected Matrix TransformToUnitCube(Matrix lightSpace)
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

        protected void CreateTransformedConvexBodyBBox(ref Matrix transform, out BoundingBox result)
        {
            result = new BoundingBox();
            for (int i = 0; i < ConvexBodyBPoints.Count; i++)
            {
                var point = ConvexBodyBPoints[i];

                Vector3 transformed;
                Vector3.TransformCoordinate(ref point, ref transform, out transformed);

                result.Merge(ref transformed);
            }
        }
    }
}
