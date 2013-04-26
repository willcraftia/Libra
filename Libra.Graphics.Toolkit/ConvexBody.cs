#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class ConvexBody
    {
        #region Polygon

        public sealed class Polygon
        {
            public List<Vector3> Vertices { get; private set; }

            public Polygon()
            {
                Vertices = new List<Vector3>(4);
            }
        }

        #endregion

        #region Edge

        struct Edge
        {
            public Vector3 Point0;

            public Vector3 Point1;

            public Edge(Vector3 point0, Vector3 point1)
            {
                Point0 = point0;
                Point1 = point1;
            }
        }

        #endregion

        // Ogre3d Vector3 と同じ値。
        const float PointEqualsTolerance = 1e-03f;

        // Ogre3d Vector3 と同じ値。
        // 即ち、Degree 1 の角度差ならば等しいベクトル方向であるとする。
        static readonly float DirectionEqualsTolerance = MathHelper.ToRadians(1);

        public List<Polygon> Polygons { get; private set; }

        Vector3[] corners;

        List<bool> outsides;

        public ConvexBody()
        {
            Polygons = new List<Polygon>(6);
            corners = new Vector3[8];
            outsides = new List<bool>(6);
        }

        public void Define(BoundingFrustum frustum)
        {
            Polygons.Clear();
            frustum.GetCorners(corners);

            // LiSPSM/Ogre3d (CCW) に合わせる。
            // 各々、配列インデックスと頂点の対応が異なる点に注意。

            // BoundingFrustum
            // 0: near-top-left
            // 1: near-top-right
            // 2: near-bottom-right
            // 3: near-bottom-left
            // 4: far-top-left
            // 5: far-top-right
            // 6: far-bottom-right
            // 7: far-bottom-left

            // LiSPSM : BoundingFrustum
            // 0: 3: near-bottom-left
            // 1: 2: near-bottom-right
            // 2: 1: near-top-right
            // 3: 0: near-top-left
            // 4: 7: far-bottom-left
            // 5: 6: far-bottom-right
            // 6: 5: far-top-right
            // 7: 4: far-top-left

            // Ogre : BoundingFrustum
            // 0: 1: near-top-right
            // 1: 0: near-top-left
            // 2: 3: near-bottom-left
            // 3: 2: near-bottom-right
            // 4: 5: far-top-right
            // 5: 4: far-top-left
            // 6: 7: far-bottom-left
            // 7: 6: far-bottom-right

            var near = new Polygon();
            near.Vertices.Add(corners[1]);
            near.Vertices.Add(corners[0]);
            near.Vertices.Add(corners[3]);
            near.Vertices.Add(corners[2]);
            Polygons.Add(near);

            var far = new Polygon();
            far.Vertices.Add(corners[4]);
            far.Vertices.Add(corners[5]);
            far.Vertices.Add(corners[6]);
            far.Vertices.Add(corners[7]);
            Polygons.Add(far);

            var left = new Polygon();
            left.Vertices.Add(corners[4]);
            left.Vertices.Add(corners[7]);
            left.Vertices.Add(corners[3]);
            left.Vertices.Add(corners[0]);
            Polygons.Add(left);

            var right = new Polygon();
            right.Vertices.Add(corners[5]);
            right.Vertices.Add(corners[1]);
            right.Vertices.Add(corners[2]);
            right.Vertices.Add(corners[6]);
            Polygons.Add(left);

            var bottom = new Polygon();
            bottom.Vertices.Add(corners[7]);
            bottom.Vertices.Add(corners[6]);
            bottom.Vertices.Add(corners[2]);
            bottom.Vertices.Add(corners[3]);
            Polygons.Add(bottom);

            var top = new Polygon();
            top.Vertices.Add(corners[5]);
            top.Vertices.Add(corners[4]);
            top.Vertices.Add(corners[0]);
            top.Vertices.Add(corners[1]);
            Polygons.Add(top);
        }

        public void Clip(BoundingBox box)
        {
            // near
            Clip(new Plane(new Vector3(0, 0, 1), box.Max));
            // far
            Clip(new Plane(new Vector3(0, 0, -1), box.Min));
            // left
            Clip(new Plane(new Vector3(-1, 0, 0), box.Min));
            // right
            Clip(new Plane(new Vector3(1, 0, 0), box.Max));
            // bottom
            Clip(new Plane(new Vector3(0, -1, 0), box.Min));
            // top
            Clip(new Plane(new Vector3(0, 1, 0), box.Max));
        }

        public void Clip(Plane plane)
        {
            // 複製。
            var sourcePolygons = new List<Polygon>(Polygons);
            // 元を削除。
            Polygons.Clear();

            // オリジナル コードでは辺をポリゴンとして扱っているが、
            // 見通しを良くするために Ogre3d 同様に Edge 構造体で管理。
            // ただし、途中のクリップ判定では、複数の交点を検出する可能性があるため、
            // 一度 Polygon クラスで頂点を集めた後、Edge へ変換している。
            // TODO
            // インスタンス生成を抑えたい。
            List<Edge> intersectEdges = new List<Edge>();

            for (int ip = 0; ip < sourcePolygons.Count; ip++)
            {
                var originalPolygon = sourcePolygons[ip];
                if (originalPolygon.Vertices.Count < 3)
                    continue;

                // TODO
                // インスタンス生成を抑えたい。
                Polygon newPolygon;
                Polygon intersectPolygon;
                Clip(ref plane, originalPolygon, out newPolygon, out intersectPolygon);

                if (3 <= newPolygon.Vertices.Count)
                {
                    // 面がある場合。

                    Polygons.Add(newPolygon);
                }

                // 交差した辺を記憶。
                if (intersectPolygon.Vertices.Count == 2)
                {
                    var edge = new Edge(
                        intersectPolygon.Vertices[0],
                        intersectPolygon.Vertices[1]);

                    intersectEdges.Add(edge);
                }

                outsides.Clear();
            }

            // 新たな多角形の構築には、少なくとも 3 つの辺が必要。
            if (3 <= intersectEdges.Count)
            {
                // TODO
                // インスタンス生成を抑えたい。
                var closingPolygon = new Polygon();
                Polygons.Add(closingPolygon);

                // TODO
                // intersectEdges は任意の位置の要素削除が発生するため、
                // 効率化が必要。
                var first = intersectEdges[0].Point0;
                var second = intersectEdges[0].Point1;
                intersectEdges.RemoveAt(0);

                Vector3 next;

                if (FindAndRemoveEdge(ref second, intersectEdges, out next))
                {
                    Vector3 edge0;
                    Vector3 edge1;
                    Vector3.Subtract(ref first, ref second, out edge0);
                    Vector3.Subtract(ref next, ref second, out edge1);
                    Vector3 polygonNormal;
                    Vector3.Cross(ref edge0, ref edge1, out polygonNormal);

                    bool frontside;
                    DirectionEquals(ref plane.Normal, ref polygonNormal, out frontside);

                    Vector3 firstVertex;
                    Vector3 currentVertex;

                    if (frontside)
                    {
                        closingPolygon.Vertices.Add(next);
                        closingPolygon.Vertices.Add(second);
                        closingPolygon.Vertices.Add(first);
                        firstVertex = next;
                        currentVertex = first;
                    }
                    else
                    {
                        closingPolygon.Vertices.Add(first);
                        closingPolygon.Vertices.Add(second);
                        closingPolygon.Vertices.Add(next);
                        firstVertex = first;
                        currentVertex = next;
                    }

                    while (0 < intersectEdges.Count)
                    {
                        if (FindAndRemoveEdge(ref currentVertex, intersectEdges, out next))
                        {
                            if (intersectEdges.Count != 0)
                            {
                                currentVertex = next;
                                closingPolygon.Vertices.Add(next);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    Polygons.Add(closingPolygon);
                }
            }
        }

        void DirectionEquals(ref Vector3 v0, ref Vector3 v1, out bool result)
        {
            float dot;
            Vector3.Dot(ref v0, ref v1, out dot);

            float angle = (float) Math.Acos(dot);

            result = (angle <= DirectionEqualsTolerance);
        }

        bool FindAndRemoveEdge(ref Vector3 target, List<Edge> edges, out Vector3 next)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].Point0.Equals(ref target, PointEqualsTolerance))
                {
                    next = edges[i].Point1;
                    edges.RemoveAt(i);
                    return true;
                }
                else if (edges[i].Point1.Equals(ref target, PointEqualsTolerance))
                {
                    next = edges[i].Point0;
                    edges.RemoveAt(i);
                    return true;
                }
            }

            next = default(Vector3);
            return false;
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

        void Clip(ref Plane plane, Polygon originalPolygon, out Polygon newPolygon, out Polygon intersectPolygon)
        {
            // 各頂点が面 plane の裏側にあるか否か。
            for (int iv = 0; iv < originalPolygon.Vertices.Count; iv++)
            {
                var v = originalPolygon.Vertices[iv];

                // 面 plane から頂点 v の距離。
                float distance;
                plane.DotCoordinate(ref v, out distance);

                // 頂点 v が面 plane の外側 (表側) にあるならば true、
                // さもなくば false。
                outsides.Add(0.0f < distance);
            }

            newPolygon = new Polygon();
            intersectPolygon = new Polygon();

            for (int iv0 = 0; iv0 < originalPolygon.Vertices.Count; iv0++)
            {
                // 二つの頂点は多角形の辺を表す。

                // 次の頂点のインデックス (末尾の次は先頭)。
                int iv1 = (iv0 + 1) % originalPolygon.Vertices.Count;

                if (outsides[iv0] && outsides[iv1])
                {
                    // 辺が面 plane の外側にあるならばスキップ。
                    continue;
                }

                if (outsides[iv0])
                {
                    // 面 plane の内側から外側へ向かう辺の場合。

                    var v0 = originalPolygon.Vertices[iv0];
                    var v1 = originalPolygon.Vertices[iv1];

                    Vector3? intersect;
                    IntersectEdgePlane(ref v0, ref v1, ref plane, out intersect);

                    if (intersect != null)
                    {
                        newPolygon.Vertices.Add(intersect.Value);
                        intersectPolygon.Vertices.Add(intersect.Value);
                    }

                    newPolygon.Vertices.Add(v1);
                }
                else if (outsides[iv1])
                {
                    // 面 plane の外側から内側へ向かう辺の場合。

                    var v0 = originalPolygon.Vertices[iv0];
                    var v1 = originalPolygon.Vertices[iv1];

                    Vector3? intersect;
                    IntersectEdgePlane(ref v0, ref v1, ref plane, out intersect);

                    if (intersect != null)
                    {
                        newPolygon.Vertices.Add(intersect.Value);
                        intersectPolygon.Vertices.Add(intersect.Value);
                    }
                }
                else
                {
                    // 辺が面の内側にある場合。

                    var v1 = originalPolygon.Vertices[iv1];

                    newPolygon.Vertices.Add(v1);
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

            float? intersect;
            ray.Intersects(ref p, out intersect);

            if (intersect != null)
            {
                // 交点。
                result = ray.GetPoint(intersect.Value);
            }
            else
            {
                result = null;
            }
        }
    }
}
