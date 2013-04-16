#region Using

using System;

#endregion

// SharpDX.BoundingFrustum から移植していたが、
// ほぼ MonoGame のコードへ置き換えているものの、
// 完全な MonoGame の移植でもない。
// MonoGame のコードは未実装が以外に多い。

namespace Libra
{
    [Serializable]
    public class BoundingFrustum : IEquatable<BoundingFrustum>
    {
        public const int CornerCount = 8;

        const int PlaneCount = 6;

        Matrix matrix;

        Vector3[] corners;

        Plane[] planes;

        public Matrix Matrix
        {
            get { return matrix; }
            set
            {
                matrix = value;
                CreatePlanes();
                CreateCorners();
            }
        }

        public Plane Near
        {
            get { return this.planes[0]; }
        }

        public Plane Far
        {
            get { return this.planes[1]; }
        }

        public Plane Left
        {
            get { return this.planes[2]; }
        }

        public Plane Right
        {
            get { return this.planes[3]; }
        }

        public Plane Top
        {
            get { return this.planes[4]; }
        }

        public Plane Bottom
        {
            get { return this.planes[5]; }
        }

        public BoundingFrustum(Matrix matrix)
        {
            this.matrix = matrix;
            CreatePlanes();
            CreateCorners();
        }

        public ContainmentType Contains(BoundingBox box)
        {
            ContainmentType result;
            Contains(ref box, out result);
            return result;
        }

        public void Contains(ref BoundingBox box, out ContainmentType result)
        {
            var intersects = false;

            for (var i = 0; i < PlaneCount; ++i)
            {
                PlaneIntersectionType planeIntersectionType;
                box.Intersects(ref this.planes[i], out planeIntersectionType);

                switch (planeIntersectionType)
                {
                    case PlaneIntersectionType.Front:
                        result = ContainmentType.Disjoint;
                        return;
                    case PlaneIntersectionType.Intersecting:
                        intersects = true;
                        break;
                }
            }

            result = intersects ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        public bool Contains(BoundingFrustum frustum)
        {
            return Contains(frustum.GetCorners()) != ContainmentType.Disjoint;
        }

        public ContainmentType Contains(BoundingSphere sphere)
        {
            ContainmentType result;
            Contains(ref sphere, out result);
            return result;
        }

        public void Contains(ref BoundingSphere sphere, out ContainmentType result)
        {
            var intersects = false;

            for (var i = 0; i < PlaneCount; ++i)
            {
                PlaneIntersectionType planeIntersectionType;
                sphere.Intersects(ref this.planes[i], out planeIntersectionType);

                switch (planeIntersectionType)
                {
                    case PlaneIntersectionType.Front:
                        result = ContainmentType.Disjoint;
                        return;
                    case PlaneIntersectionType.Intersecting:
                        intersects = true;
                        break;
                }
            }
            
            result = intersects ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        public ContainmentType Contains(Vector3 point)
        {
            ContainmentType result;
            Contains(ref point, out result);
            return result;
        }

        public void Contains(ref Vector3 point, out ContainmentType result)
        {
            for (int i = 0; i < PlaneCount; ++i)
            {
                if (0 < ClassifyPoint(ref point, ref planes[i]))
                {
                    result = ContainmentType.Disjoint;
                    return;
                }
            }

            result = ContainmentType.Contains;
        }

        public ContainmentType Contains(Vector3[] points)
        {
            ContainmentType result;
            Contains(points, out result);
            return result;
        }

        public void Contains(Vector3[] points, out ContainmentType result)
        {
            if (points == null) throw new ArgumentNullException("points");

            var containsAny = false;
            var containsAll = true;
            for (int i = 0; i < points.Length; i++)
            {
                ContainmentType pointContainmentResult;
                Contains(ref points[i], out pointContainmentResult);
                
                switch (pointContainmentResult)
                {
                    case ContainmentType.Contains:
                    case ContainmentType.Intersects:
                        containsAny = true;
                        break;
                    case ContainmentType.Disjoint:
                        containsAll = false;
                        break;
                }
            }
            if (containsAny)
            {
                if (containsAll)
                {
                    result = ContainmentType.Contains;
                    return;
                }
                else
                {
                    result = ContainmentType.Intersects;
                    return;
                }
            }
            else
            {
                result = ContainmentType.Disjoint;
                return;
            }
        }

        public Vector3[] GetCorners()
        {
            return (Vector3[]) corners.Clone();
        }

        public void GetCorners(Vector3[] results)
        {
            if (results == null) throw new ArgumentNullException("results");
            if (results.Length < CornerCount) throw new ArgumentOutOfRangeException("results");

            corners.CopyTo(results, 0);
        }

        public bool Intersects(BoundingBox box)
        {
            bool result;
            Intersects(ref box, out result);
            return result;
        }

        public void Intersects(ref BoundingBox box, out bool result)
        {
            ContainmentType containmentType;
            Contains(ref box, out containmentType);

            result = containmentType != ContainmentType.Disjoint;
        }

        // TODO
        //
        // public bool Intersects(BoundingFrustum frustum) 未実装。

        public bool Intersects(BoundingSphere sphere)
        {
            bool result;
            Intersects(ref sphere, out result);
            return result;
        }
        
        public void Intersects(ref BoundingSphere sphere, out bool result)
        {
            ContainmentType containmentType;
            Contains(ref sphere, out containmentType);

            result = containmentType != ContainmentType.Disjoint;
        }

        public PlaneIntersectionType Intersects(Plane plane)
        {
            PlaneIntersectionType result;
            Intersects(ref plane, out result);
            return result;
        }

        public void Intersects(ref Plane plane, out PlaneIntersectionType result)
        {
            result = PlaneIntersectsPoints(ref plane, GetCorners());
        }

        public float? Intersects(Ray ray)
        {
            float? result;
            Intersects(ref ray, out result);
            return result;
        }
        
        public void Intersects(ref Ray ray, out float? result)
        {
            if (Contains(ray.Position) != ContainmentType.Disjoint)
            {
                float nearstPlaneDistance = float.MaxValue;
                for (int i = 0; i < 6; i++)
                {
                    float distance;
                    if (RayIntersectsPlane(ref ray, ref planes[i], out distance) && distance < nearstPlaneDistance)
                    {
                        nearstPlaneDistance = distance;
                    }
                }

                result = nearstPlaneDistance;
                return;
            }
            else
            {
                float minDist = float.MaxValue;
                float maxDist = float.MinValue;
                for (int i = 0; i < 6; i++)
                {
                    float distance;
                    if (RayIntersectsPlane(ref ray, ref planes[i], out distance))
                    {
                        minDist = Math.Min(minDist, distance);
                        maxDist = Math.Max(maxDist, distance);
                    }
                }

                Vector3 minPoint = ray.Position + ray.Direction * minDist;
                Vector3 maxPoint = ray.Position + ray.Direction * maxDist;
                Vector3 center = (minPoint + maxPoint) / 2f;

                ContainmentType centerContainmentResult;
                Contains(ref center, out centerContainmentResult);
                if (centerContainmentResult != ContainmentType.Disjoint)
                {
                    result = minDist;
                    return;
                }
                else
                {
                    result = null;
                    return;
                }
            }
        }

        void CreateCorners()
        {
            corners = new Vector3[CornerCount];
            IntersectionPoint(ref planes[0], ref planes[2], ref planes[4], out corners[0]);
            IntersectionPoint(ref planes[0], ref planes[3], ref planes[4], out corners[1]);
            IntersectionPoint(ref planes[0], ref planes[3], ref planes[5], out corners[2]);
            IntersectionPoint(ref planes[0], ref planes[2], ref planes[5], out corners[3]);
            IntersectionPoint(ref planes[1], ref planes[2], ref planes[4], out corners[4]);
            IntersectionPoint(ref planes[1], ref planes[3], ref planes[4], out corners[5]);
            IntersectionPoint(ref planes[1], ref planes[3], ref planes[5], out corners[6]);
            IntersectionPoint(ref planes[1], ref planes[2], ref planes[5], out corners[7]);
        }

        void CreatePlanes()
        {
            planes = new Plane[PlaneCount];
            planes[0] = new Plane(-matrix.M13, -matrix.M23, -matrix.M33, -matrix.M43);
            planes[1] = new Plane(matrix.M13 - matrix.M14, matrix.M23 - matrix.M24, matrix.M33 - matrix.M34, matrix.M43 - matrix.M44);
            planes[2] = new Plane(-matrix.M14 - matrix.M11, -matrix.M24 - matrix.M21, -matrix.M34 - matrix.M31, -matrix.M44 - matrix.M41);
            planes[3] = new Plane(matrix.M11 - matrix.M14, matrix.M21 - matrix.M24, matrix.M31 - matrix.M34, matrix.M41 - matrix.M44);
            planes[4] = new Plane(matrix.M12 - matrix.M14, matrix.M22 - matrix.M24, matrix.M32 - matrix.M34, matrix.M42 - matrix.M44);
            planes[5] = new Plane(-matrix.M14 - matrix.M12, -matrix.M24 - matrix.M22, -matrix.M34 - matrix.M32, -matrix.M44 - matrix.M42);

            planes[0].Normalize();
            planes[1].Normalize();
            planes[2].Normalize();
            planes[3].Normalize();
            planes[4].Normalize();
            planes[5].Normalize();
        }

        static void IntersectionPoint(ref Plane a, ref Plane b, ref Plane c, out Vector3 result)
        {
            // Formula used
            //                d1 ( N2 * N3 ) + d2 ( N3 * N1 ) + d3 ( N1 * N2 )
            //P =   -------------------------------------------------------------------------
            //                             N1 . ( N2 * N3 )
            //
            // Note: N refers to the normal, d refers to the displacement. '.' means dot product. '*' means cross product

            Vector3 cross;
            Vector3.Cross(ref b.Normal, ref c.Normal, out cross);

            float f;
            Vector3.Dot(ref a.Normal, ref cross, out f);
            f *= -1.0f;

            Vector3.Cross(ref b.Normal, ref c.Normal, out cross);
            Vector3 v1;
            Vector3.Multiply(ref cross, a.D, out v1);

            Vector3.Cross(ref c.Normal, ref a.Normal, out cross);
            Vector3 v2;
            Vector3.Multiply(ref cross, b.D, out v2);

            Vector3.Cross(ref a.Normal, ref b.Normal, out cross);
            Vector3 v3;
            Vector3.Multiply(ref cross, c.D, out v3);

            result.X = (v1.X + v2.X + v3.X) / f;
            result.Y = (v1.Y + v2.Y + v3.Y) / f;
            result.Z = (v1.Z + v2.Z + v3.Z) / f;
        }

        static float ClassifyPoint(ref Vector3 point, ref Plane plane)
        {
            return point.X * plane.Normal.X + point.Y * plane.Normal.Y + point.Z * plane.Normal.Z + plane.D;
        }

        static PlaneIntersectionType PlaneIntersectsPoints(ref Plane plane, Vector3[] points)
        {
            var result = PlaneIntersectsPoint(ref plane, ref points[0]);
            for (int i = 1; i < points.Length; i++)
                if (PlaneIntersectsPoint(ref plane, ref points[i]) != result)
                    return PlaneIntersectionType.Intersecting;
            return result;
        }

        static PlaneIntersectionType PlaneIntersectsPoint(ref Plane plane, ref Vector3 point)
        {
            // SharpDX.Collision.PlaneIntersectsPoint より。

            float distance;
            Vector3.Dot(ref plane.Normal, ref point, out distance);
            distance += plane.D;

            if (distance > 0f)
                return PlaneIntersectionType.Front;

            if (distance < 0f)
                return PlaneIntersectionType.Back;

            return PlaneIntersectionType.Intersecting;
        }

        static bool RayIntersectsPlane(ref Ray ray, ref Plane plane, out float distance)
        {
            // SharpDX.Collision.RayIntersectsPlane より。

            float direction;
            Vector3.Dot(ref plane.Normal, ref ray.Direction, out direction);

            if (Math.Abs(direction) < MathHelper.ZeroTolerance)
            {
                distance = 0f;
                return false;
            }

            float position;
            Vector3.Dot(ref plane.Normal, ref ray.Position, out position);
            distance = (-plane.D - position) / direction;

            if (distance < 0f)
            {
                if (distance < -MathHelper.ZeroTolerance)
                {
                    distance = 0;
                    return false;
                }

                distance = 0f;
            }

            return true;
        }

        #region IEquatable

        public static bool operator ==(BoundingFrustum left, BoundingFrustum right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoundingFrustum left, BoundingFrustum right)
        {
            return !left.Equals(right);
        }

        public bool Equals(BoundingFrustum other)
        {
            return matrix == other.matrix;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((BoundingFrustum) obj);
        }

        public override int GetHashCode()
        {
            return matrix.GetHashCode();
        }

        #endregion
    }
}
