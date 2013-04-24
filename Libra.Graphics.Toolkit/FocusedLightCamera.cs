#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    public class FocusedLightCamera : LightCamera
    {
        // 光の方向 (光源からの光の進行方向)
        public Vector3 LightDirection;

        List<Vector3> convexBodyBPoints;

        List<Vector3> transformedConvexBodyBPoints;

        Vector3[] corners;

        public FocusedLightCamera()
        {
            LightDirection = Vector3.Down;
            corners = new Vector3[BoundingBox.CornerCount];
            convexBodyBPoints = new List<Vector3>();
            transformedConvexBodyBPoints = new List<Vector3>();
        }

        public void SetConvexBodyBPoints(IEnumerable<Vector3> points)
        {
            if (points == null) throw new ArgumentNullException("points");

            convexBodyBPoints.Clear();

            foreach (var point in points)
                convexBodyBPoints.Add(point);
        }

        public void SetConvexBodyB(ConvexBody convexBodyB, BoundingBox sceneBox)
        {
            convexBodyBPoints.Clear();

            for (int ip = 0; ip < convexBodyB.Polygons.Count; ip++)
            {
                var polygon = convexBodyB.Polygons[ip];

                for (int iv = 0; iv < polygon.Vertices.Count; iv++)
                {
                    convexBodyBPoints.Add(polygon.Vertices[iv]);
                }
            }

            int count = convexBodyBPoints.Count;
            for (int i = 0; i < count; i++)
            {
                var ray = new Ray(convexBodyBPoints[i], LightDirection);
                float? intersect;
                ray.Intersects(ref sceneBox, out intersect);
                if (intersect != null)
                {
                    var newPoint = ray.Position + ray.Direction * intersect.Value;
                    convexBodyBPoints.Add(newPoint);
                }
            }
        }

        public override void Update()
        {
            // USM (Uniform Shadow Mapping) による行列算出。

            var position = Vector3.Zero;
            var target = LightDirection;
            var up = Vector3.Up;

            // ライト ビュー行列。
            Matrix tempLightView;
            Matrix.CreateLookAt(ref position, ref target, ref up, out tempLightView);

            // 仮ライト空間における凸体 B の AABB。
            BoundingBox lightConvexBodyBBox;
            CreateTransformedConvexBodyBBox(ref tempLightView, out lightConvexBodyBBox);

            Vector3 boxSize;
            Vector3.Subtract(ref lightConvexBodyBBox.Max, ref lightConvexBodyBBox.Min, out boxSize);

            Vector3 halfBoxSize;
            Vector3.Multiply(ref boxSize, 0.5f, out halfBoxSize);

            // 光源から見て最も近い面 (Min.Z) の中心 (XY について半分の位置) を光源位置に決定。
            Vector3 lightPosition;
            Vector3.Add(ref lightConvexBodyBBox.Min, ref halfBoxSize, out lightPosition);
            lightPosition.Z = lightConvexBodyBBox.Min.Z;

            // 算出した光源位置は仮ライト空間にあるため、これをワールド空間へ変換。
            Matrix lightViewInv;
            Matrix.Invert(ref tempLightView, out lightViewInv);
            Vector3.Transform(ref lightPosition, ref lightViewInv, out lightPosition);

            Vector3.Add(ref lightPosition, ref LightDirection, out target);

            // 得られた光源情報からライト ビュー行列を算出。
            Matrix.CreateLookAt(ref lightPosition, ref target, ref up, out LightView);

            // 仮ライト空間の境界ボックスのサイズで正射影として光源射影行列を算出。
            Matrix.CreateOrthographic(boxSize.X, boxSize.Y, -boxSize.Z, boxSize.Z, out LightProjection);

            // クリア。
            convexBodyBPoints.Clear();
        }

        protected void CreateTransformedConvexBodyBBox(ref Matrix transform, out BoundingBox result)
        {
            TransformConvexBodyBPoints(ref transform);
            BoundingBox.CreateFromPoints(transformedConvexBodyBPoints, out result);
        }

        void TransformConvexBodyBPoints(ref Matrix matrix)
        {
            transformedConvexBodyBPoints.Clear();

            int count = convexBodyBPoints.Count;

            for (int i = 0; i < count; i++)
            {
                var source = convexBodyBPoints[i];

                Vector3 destination;
                Vector3.TransformCoordinate(ref source, ref matrix, out destination);

                transformedConvexBodyBPoints.Add(destination);
            }
        }
    }
}
