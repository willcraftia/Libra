#region Using

using System;
using System.Collections.Generic;
using Libra.Collections;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class ConvexBody
    {
        #region Polygon

        public sealed class Polygon
        {
            StructList<Vector3> vertices;

            public int VertexCount
            {
                get { return vertices.Count; }
            }

            public Polygon()
            {
                vertices = new StructList<Vector3>(4);
            }

            public void GetVertex(int index, out Vector3 result)
            {
                vertices.GetItem(index, out result);
            }

            public void AddVertex(ref Vector3 vertex)
            {
                vertices.Add(ref vertex);
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

            public override string ToString()
            {
                return "{Point0: " + Point0 + " Point1:" + Point1 + "}";
            }
        }

        #endregion

        // Ogre3d Vector3 と同じ値。
        const float PointEqualsTolerance = 1e-03f;

        // Ogre3d Vector3 と同じ値。
        // 即ち、Degree 1 の角度差ならば等しいベクトル方向であるとする。
        static readonly float DirectionEqualsTolerance = MathHelper.ToRadians(1);

        Vector3[] corners;

        List<bool> outsides;

        StructList<Edge> intersectEdges;

        public List<Polygon> Polygons { get; private set; }

        public ConvexBody()
        {
            Polygons = new List<Polygon>(6);
            corners = new Vector3[8];
            outsides = new List<bool>(6);
            intersectEdges = new StructList<Edge>();
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
            near.AddVertex(ref corners[1]);
            near.AddVertex(ref corners[0]);
            near.AddVertex(ref corners[3]);
            near.AddVertex(ref corners[2]);
            Polygons.Add(near);

            var far = new Polygon();
            far.AddVertex(ref corners[4]);
            far.AddVertex(ref corners[5]);
            far.AddVertex(ref corners[6]);
            far.AddVertex(ref corners[7]);
            Polygons.Add(far);

            var left = new Polygon();
            left.AddVertex(ref corners[4]);
            left.AddVertex(ref corners[7]);
            left.AddVertex(ref corners[3]);
            left.AddVertex(ref corners[0]);
            Polygons.Add(left);

            var right = new Polygon();
            right.AddVertex(ref corners[5]);
            right.AddVertex(ref corners[1]);
            right.AddVertex(ref corners[2]);
            right.AddVertex(ref corners[6]);
            Polygons.Add(left);

            var bottom = new Polygon();
            bottom.AddVertex(ref corners[7]);
            bottom.AddVertex(ref corners[6]);
            bottom.AddVertex(ref corners[2]);
            bottom.AddVertex(ref corners[3]);
            Polygons.Add(bottom);

            var top = new Polygon();
            top.AddVertex(ref corners[5]);
            top.AddVertex(ref corners[4]);
            top.AddVertex(ref corners[0]);
            top.AddVertex(ref corners[1]);
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
            // 見通しを良くするため Edge 構造体で管理。
            // ただし、途中のクリップ判定では、複数の交点を検出する可能性があるため、
            // 一度 Polygon クラスで頂点を集めた後、Edge へ変換している。
            intersectEdges.Clear();

            for (int ip = 0; ip < sourcePolygons.Count; ip++)
            {
                var originalPolygon = sourcePolygons[ip];
                if (originalPolygon.VertexCount < 3)
                    continue;

                Polygon newPolygon;
                Polygon intersectPolygon;
                Clip(ref plane, originalPolygon, out newPolygon, out intersectPolygon);

                if (3 <= newPolygon.VertexCount)
                {
                    // 面がある場合。

                    Polygons.Add(newPolygon);
                }

                // 交差した辺を記憶。
                if (intersectPolygon.VertexCount == 2)
                {
                    Vector3 v0;
                    Vector3 v1;
                    intersectPolygon.GetVertex(0, out v0);
                    intersectPolygon.GetVertex(1, out v1);

                    var edge = new Edge(v0, v1);

                    intersectEdges.Add(ref edge);
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

                Edge lastEdge;
                intersectEdges.GetLastItem(out lastEdge);
                intersectEdges.RemoveLast();

                Vector3 first = lastEdge.Point0;
                Vector3 second = lastEdge.Point1;

                Vector3 next;

                if (FindPointAndRemoveEdge(ref second, intersectEdges, out next))
                {
                    // 交差する二つの辺から多角形の法線を算出。
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
                        // 
                        closingPolygon.AddVertex(ref next);
                        closingPolygon.AddVertex(ref second);
                        closingPolygon.AddVertex(ref first);
                        firstVertex = next;
                        currentVertex = first;
                    }
                    else
                    {
                        closingPolygon.AddVertex(ref first);
                        closingPolygon.AddVertex(ref second);
                        closingPolygon.AddVertex(ref next);
                        firstVertex = first;
                        currentVertex = next;
                    }

                    while (0 < intersectEdges.Count)
                    {
                        if (FindPointAndRemoveEdge(ref currentVertex, intersectEdges, out next))
                        {
                            if (intersectEdges.Count != 0)
                            {
                                currentVertex = next;
                                closingPolygon.AddVertex(ref next);
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

        bool FindPointAndRemoveEdge(ref Vector3 point, StructList<Edge> edges, out Vector3 another)
        {
            another = default(Vector3);
            int index = -1;

            for (int i = 0; i < edges.Count; i++)
            {
                Edge edge;
                edges.GetItem(i, out edge);

                if (edge.Point0.Equals(ref point, PointEqualsTolerance))
                {
                    another = edge.Point1;
                    index = i;
                    break;
                }
                else if (edge.Point1.Equals(ref point, PointEqualsTolerance))
                {
                    another = edge.Point0;
                    index = i;
                    break;
                }
            }

            // リスト内部における部分的な配列複製を回避するため、
            // 対象となった要素を末尾と入れ替えた後、末尾を対象に削除。
            if (0 <= index)
            {
                edges.SwapWithLast(index);
                edges.RemoveLast();
                return true;
            }
            else
            {
                return false;
            }
        }

        void Clip(ref Plane plane, Polygon originalPolygon, out Polygon newPolygon, out Polygon intersectPolygon)
        {
            // 各頂点が面 plane の裏側にあるか否か。
            for (int iv = 0; iv < originalPolygon.VertexCount; iv++)
            {
                Vector3 v;
                originalPolygon.GetVertex(iv, out v);

                // 面 plane から頂点 v の距離。
                float distance;
                plane.DotCoordinate(ref v, out distance);

                // 頂点 v が面 plane の外側 (表側) にあるならば true、
                // さもなくば false。
                outsides.Add(0.0f < distance);
            }

            newPolygon = new Polygon();
            intersectPolygon = new Polygon();

            for (int iv0 = 0; iv0 < originalPolygon.VertexCount; iv0++)
            {
                // 二つの頂点は多角形の辺を表す。

                // 次の頂点のインデックス (末尾の次は先頭)。
                int iv1 = (iv0 + 1) % originalPolygon.VertexCount;

                if (outsides[iv0] && outsides[iv1])
                {
                    // 辺が面 plane の外側にあるならばスキップ。
                    continue;
                }

                if (outsides[iv0])
                {
                    // 面 plane の外側から内側へ向かう辺の場合。

                    Vector3 v0;
                    Vector3 v1;
                    originalPolygon.GetVertex(iv0, out v0);
                    originalPolygon.GetVertex(iv1, out v1);

                    Vector3? intersect;
                    IntersectEdgeAndPlane(ref v0, ref v1, ref plane, out intersect);

                    if (intersect != null)
                    {
                        Vector3 intersectV = intersect.Value;
                        newPolygon.AddVertex(ref intersectV);
                        intersectPolygon.AddVertex(ref intersectV);
                    }

                    newPolygon.AddVertex(ref v1);
                }
                else if (outsides[iv1])
                {
                    // 面 plane の内側から外側へ向かう辺の場合。

                    Vector3 v0;
                    Vector3 v1;
                    originalPolygon.GetVertex(iv0, out v0);
                    originalPolygon.GetVertex(iv1, out v1);

                    Vector3? intersect;
                    IntersectEdgeAndPlane(ref v0, ref v1, ref plane, out intersect);

                    if (intersect != null)
                    {
                        Vector3 intersectV = intersect.Value;
                        newPolygon.AddVertex(ref intersectV);
                        intersectPolygon.AddVertex(ref intersectV);
                    }
                }
                else
                {
                    // 辺が面の内側にある場合。

                    Vector3 v1;
                    originalPolygon.GetVertex(iv1, out v1);

                    newPolygon.AddVertex(ref v1);
                }
            }
        }

        /// <summary>
        /// 辺と平面の交差を判定します。
        /// </summary>
        /// <param name="point0">point1 と対をなす辺の点。</param>
        /// <param name="point1">point0 と対をなす辺の点。</param>
        /// <param name="plane">平面。</param>
        /// <param name="result">
        /// 交点 (辺と平面が交差する場合)、null (それ以外の場合)。
        /// </param>
        void IntersectEdgeAndPlane(ref Vector3 point0, ref Vector3 point1, ref Plane plane, out Vector3? result)
        {
            // 辺の方向。
            var direction = point0 - point1;
            direction.Normalize();

            // 辺と面 p との交差を判定。
            var ray = new Ray(point1, direction);

            float? intersect;
            ray.Intersects(ref plane, out intersect);

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
