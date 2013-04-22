#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    public class FocusedLightCamera : LightCamera
    {
        #region Polygon

        protected sealed class Polygon
        {
            public List<Vector3> Vertices;

            public Polygon()
            {
                Vertices = new List<Vector3>(4);
            }
        }

        #endregion

        #region Edge

        protected struct Edge
        {
            public Vector3 V0;

            public Vector3 V1;

            public Edge(Vector3 v0, Vector3 v1)
            {
                V0 = v0;
                V1 = v1;
            }
        }

        #endregion

        #region ConvexBody

        protected class ConvexBody
        {
            List<Polygon> polygons;

            Vector3[] corners;

            List<bool> outsides;

            public ConvexBody()
            {
                polygons = new List<Polygon>(6);
                corners = new Vector3[8];
                outsides = new List<bool>(6);
            }

            public void Define(BoundingFrustum frustum)
            {
                polygons.Clear();
                frustum.GetCorners(corners);

                // LiSPSM の C コードに合わせる。
                // 即ち、CCW での並び (BoundingFrustum は CW)。
                // 0: 3: near-bottom-left
                // 1: 2: near-bottom-right
                // 2: 1: near-top-right
                // 3: 0: near-top-left
                // 4: 7: far-bottom-left
                // 5: 6: far-bottom-right
                // 6: 5: far-top-right
                // 7: 4: far-top-left

                var near = new Polygon();
                near.Vertices.Add(corners[3]);
                near.Vertices.Add(corners[2]);
                near.Vertices.Add(corners[1]);
                near.Vertices.Add(corners[0]);
                polygons.Add(near);

                var far = new Polygon();
                far.Vertices.Add(corners[4]);
                far.Vertices.Add(corners[5]);
                far.Vertices.Add(corners[6]);
                far.Vertices.Add(corners[7]);
                polygons.Add(far);

                var left = new Polygon();
                left.Vertices.Add(corners[3]);
                left.Vertices.Add(corners[0]);
                left.Vertices.Add(corners[4]);
                left.Vertices.Add(corners[7]);
                polygons.Add(left);

                var right = new Polygon();
                right.Vertices.Add(corners[2]);
                right.Vertices.Add(corners[6]);
                right.Vertices.Add(corners[5]);
                right.Vertices.Add(corners[1]);
                polygons.Add(left);

                var bottom = new Polygon();
                bottom.Vertices.Add(corners[7]);
                bottom.Vertices.Add(corners[6]);
                bottom.Vertices.Add(corners[2]);
                bottom.Vertices.Add(corners[3]);
                polygons.Add(bottom);

                var top = new Polygon();
                top.Vertices.Add(corners[5]);
                top.Vertices.Add(corners[4]);
                top.Vertices.Add(corners[0]);
                top.Vertices.Add(corners[1]);
                polygons.Add(top);
            }

            public void Clip(BoundingBox box)
            {
                // near
                Clip(new Plane(new Vector3(0, 0, 1), box.Max.Z));
                // far
                Clip(new Plane(new Vector3(0, 0, -1), box.Min.Z));
                // left
                Clip(new Plane(new Vector3(-1, 0, 0), box.Min.X));
                // right
                Clip(new Plane(new Vector3(1, 0, 0), box.Max.X));
                // bottom
                Clip(new Plane(new Vector3(0, -1, 0), box.Min.Y));
                // top
                Clip(new Plane(new Vector3(0, 1, 0), box.Max.Y));
            }

            public void Clip(Plane plane)
            {
                // 複製。
                var sourcePolygons = new List<Polygon>(polygons);
                // 元を削除。
                polygons.Clear();

                // オリジナル コードに合わせて頂点管理の単位を Polygon クラスとしているが、
                // Polygon クラスはそれ単体でポリゴンを表しているとは限らない。
                // 例えば、交差判定で現れる頂点の管理では、
                // Polygon クラスは 2 頂点で辺を管理していたりする。
                // TODO
                // インスタンス生成を抑えたい。
                List<Polygon> intersectPolygons = new List<Polygon>();

                for (int ip = 0; ip < sourcePolygons.Count; ip++)
                {
                    var inPolygon = sourcePolygons[ip];
                    if (inPolygon.Vertices.Count < 3)
                        continue;

                    // TODO
                    // インスタンス生成を抑えたい。
                    Polygon outPolygon;
                    Polygon intersectPolygon;
                    Clip(ref plane, inPolygon, out outPolygon, out intersectPolygon);

                    if (3 <= outPolygon.Vertices.Count)
                    {
                        // 面がある場合。

                        polygons.Add(outPolygon);
                    }

                    // 交差した辺を記憶。
                    if (intersectPolygon.Vertices.Count == 2)
                    {
                        intersectPolygons.Add(intersectPolygon);
                    }

                    outsides.Clear();
                }

                // 少なくとも 3 つの辺が必要。
                if (3 <= intersectPolygons.Count)
                {
                    // TODO
                    // インスタンス生成を抑えたい。
                    var newPolygon = new Polygon();
                    polygons.Add(newPolygon);

                    var intersectPolygon = intersectPolygons[intersectPolygons.Count - 1];
                    intersectPolygons.RemoveAt(intersectPolygons.Count - 1);

                    newPolygon.Vertices.Add(intersectPolygon.Vertices[0]);
                    newPolygon.Vertices.Add(intersectPolygon.Vertices[1]);

                    while (0 < intersectPolygons.Count)
                    {
                        // ポリゴンに設定した最後の頂点。
                        var lastVertex = newPolygon.Vertices[newPolygon.Vertices.Count - 1];
                        // 最後の頂点に等しい頂点を含む辺を検索し、末尾へ移動。
                        var index = FindSamePointAndSwapWithLast(intersectPolygons, ref lastVertex);
                        
                        if (0 <= index)
                        {
                            // 末尾にある辺。
                            var targetPolygon = intersectPolygons[intersectPolygons.Count - 1];

                            // 先の検索で等しいとされた頂点の次の頂点を取得。
                            var targetVertex = targetPolygon.Vertices[(index + 1) % 2];

                            newPolygon.Vertices.Add(targetVertex);
                        }

                        // 利用していてもしていなくても末尾の辺を削除。
                        intersectPolygons.RemoveAt(intersectPolygons.Count - 1);
                    }

                    // 最後の頂点は開始頂点と同じであるため、最後に削除する。
                    newPolygon.Vertices.RemoveAt(newPolygon.Vertices.Count - 1);
                }
            }

            int FindSamePointAndSwapWithLast(List<Polygon> polygons, ref Vector3 vertex)
            {
                for (int i = polygons.Count; 0 < i; i--)
                {
                    var polygon = polygons[i - 1];
                    var index = FindSamePoint(polygon, ref vertex);
                    if (0 <= index)
                    {
                        var temp = polygon.Vertices[index];
                        polygon.Vertices[index] = polygon.Vertices[polygon.Vertices.Count - 1];
                        polygon.Vertices[polygon.Vertices.Count - 1] = temp;

                        return index;
                    }
                }

                return -1;
            }

            int FindSamePoint(Polygon polygon, ref Vector3 vertex)
            {
                for (int i = 0; i < polygon.Vertices.Count; i++)
                {
                    var v = polygon.Vertices[i];
                    if (v.Equals(ref vertex, MathHelper.ZeroTolerance))
                        return i;
                }

                return -1;
            }

            void Clip(ref Plane plane, Polygon inPolygon, out Polygon outPolygon, out Polygon intersectPolygon)
            {
                // 各頂点が面 plane の裏側にあるか否か。
                for (int iv = 0; iv < inPolygon.Vertices.Count; iv++)
                {
                    var v = inPolygon.Vertices[iv];

                    // 面 plane から頂点 v の距離。
                    float distance;
                    plane.DotCoordinate(ref v, out distance);

                    // 頂点 v が面 plane の外側 (表側) にあるならば true、
                    // さもなくば  false。
                    outsides.Add(0.0f < distance);
                }

                outPolygon = new Polygon();
                intersectPolygon = new Polygon();

                for (int iv0 = 0; iv0 < inPolygon.Vertices.Count; iv0++)
                {
                    // 二つの頂点は多角形の辺を表す。

                    // 次の頂点のインデックス (末尾の次は先頭)。
                    int iv1 = (iv0 + 1) % inPolygon.Vertices.Count;

                    if (outsides[iv0] && outsides[iv1])
                    {
                        // 辺が面 plane の外側にあるならばスキップ。
                        continue;
                    }

                    var v0 = inPolygon.Vertices[iv0];
                    var v1 = inPolygon.Vertices[iv1];

                    if (outsides[iv0])
                    {
                        // 面 plane の内側から外側へ向かう辺の場合。

                        Vector3? intersect;
                        IntersectEdgePlane(ref v0, ref v1, ref plane, out intersect);

                        if (intersect != null)
                        {
                            outPolygon.Vertices.Add(intersect.Value);
                            intersectPolygon.Vertices.Add(intersect.Value);
                        }

                        outPolygon.Vertices.Add(v1);
                    }
                    else if (outsides[iv1])
                    {
                        // 面 plane の外側から内側へ向かう辺の場合。

                        Vector3? intersect;
                        IntersectEdgePlane(ref v0, ref v1, ref plane, out intersect);

                        if (intersect != null)
                        {
                            outPolygon.Vertices.Add(intersect.Value);
                            intersectPolygon.Vertices.Add(intersect.Value);
                        }
                    }
                    else
                    {
                        // 辺が面の内側にある場合。

                        outPolygon.Vertices.Add(v1);
                    }
                }
            }

            void IntersectEdgePlane(ref Vector3 v0, ref Vector3 v1, ref Plane p, out Vector3? result)
            {
                // 辺の方向。
                var direction = v0 - v1;
                direction.Normalize();

                // 辺と面 p との交差を判定。
                var ray = new Ray(v1, direction);

                float? distance;
                ray.Intersects(ref p, out distance);

                if (distance != null)
                {
                    // 交点。
                    result = ray.Position + direction * distance.Value;
                }
                else
                {
                    result = null;
                }
            }
        }

        #endregion

        // 光の方向 (光源からの光の進行方向)
        public Vector3 LightDirection;

        public Matrix EyeView;

        public Matrix EyeProjection;

        protected List<Vector3> lightVolumePoints;

        Vector3[] corners;

        public FocusedLightCamera()
        {
            LightDirection = Vector3.Down;
            LightViewProjection = Matrix.Identity;
            corners = new Vector3[BoundingBox.CornerCount];
            lightVolumePoints = new List<Vector3>();
        }

        public void AddLightVolumePoint(Vector3 point)
        {
            lightVolumePoints.Add(point);
        }

        public void AddLightVolumePoint(ref Vector3 point)
        {
            lightVolumePoints.Add(point);
        }

        public void AddLightVolumePoints(IEnumerable<Vector3> points)
        {
            if (points == null) throw new ArgumentNullException("points");

            foreach (var point in points)
                lightVolumePoints.Add(point);
        }

        public void AddLightVolumePoints(BoundingBox box)
        {
            AddLightVolumePoints(ref box);
        }

        public void AddLightVolumePoints(ref BoundingBox box)
        {
            box.GetCorners(corners);
            AddLightVolumePoints(corners);
        }

        public void AddLightVolumePoints(BoundingFrustum frustum)
        {
            frustum.GetCorners(corners);
            AddLightVolumePoints(corners);
        }

        public override void Update()
        {
            // USM (Uniform Shadow Mapping) による行列算出。

            var position = Vector3.Zero;
            var target = LightDirection;
            var up = Vector3.Up;

            // 仮光源ビュー行列。
            Matrix tempLightView;
            Matrix.CreateLookAt(ref position, ref target, ref up, out tempLightView);

            // 指定されている点を含む境界ボックス。
            BoundingBox tempLightBox;
            BoundingBox.CreateFromPoints(lightVolumePoints, out tempLightBox);

            // 境界ボックスの頂点を仮光源空間へ変換。
            tempLightBox.GetCorners(corners);
            for (int i = 0; i < corners.Length; i++)
                Vector3.Transform(ref corners[i], ref tempLightView, out corners[i]);

            // 仮光源空間での境界ボックス。
            BoundingBox lightBox;
            BoundingBox.CreateFromPoints(corners, out lightBox);

            Vector3 boxSize;
            Vector3.Subtract(ref lightBox.Max, ref lightBox.Min, out boxSize);

            Vector3 halfBoxSize;
            Vector3.Multiply(ref boxSize, 0.5f, out halfBoxSize);

            // 光源から見て最も近い面 (Min.Z) の中心 (XY について半分の位置) を光源位置に決定。
            Vector3 lightPosition;
            Vector3.Add(ref lightBox.Min, ref halfBoxSize, out lightPosition);
            lightPosition.Z = lightBox.Min.Z;

            // 算出した光源位置は仮光源空間にあるため、これをワールド空間へ変換。
            Matrix lightViewInv;
            Matrix.Invert(ref tempLightView, out lightViewInv);
            Vector3.Transform(ref lightPosition, ref lightViewInv, out lightPosition);

            Vector3.Add(ref lightPosition, ref LightDirection, out target);

            // 得られた光源情報から光源ビュー行列を算出。
            Matrix.CreateLookAt(ref lightPosition, ref target, ref up, out LightView);

            // 仮光源空間の境界ボックスのサイズで正射影として光源射影行列を算出。
            Matrix.CreateOrthographic(boxSize.X, boxSize.Y, -boxSize.Z, boxSize.Z, out LightProjection);

            Matrix.Multiply(ref LightView, ref LightProjection, out LightViewProjection);

            // クリア。
            lightVolumePoints.Clear();
        }
    }
}
