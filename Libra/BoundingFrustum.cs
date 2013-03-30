#region Using

using System;

#endregion

// SharpDX.BoundingFrustum から移植。
// 一部インタフェースを XNA 形式へ変更。
// 一部ロジックを変更。

namespace Libra
{
    [Serializable]
    public class BoundingFrustum : IEquatable<BoundingFrustum>
    {
        public const int CornerCount = 8;

        Matrix matrix;
        
        Plane near;
        
        Plane far;
        
        Plane left;
        
        Plane right;
        
        Plane top;
        
        Plane bottom;

        public Matrix Matrix
        {
            get { return matrix; }
            set
            {
                matrix = value;
                GetPlanesFromMatrix(ref matrix, out near, out far, out left, out right, out top, out bottom);
            }
        }
        public Plane Near
        {
            get { return near; }
        }

        public Plane Far
        {
            get { return far; }
        }
        
        public Plane Left
        {
            get { return left; }
        }
        
        public Plane Right
        {
            get { return right; }
        }

        public Plane Top
        {
            get { return top; }
        }
        
        public Plane Bottom
        {
            get { return bottom; }
        }

        public BoundingFrustum(Matrix matrix)
        {
            this.matrix = matrix;
            GetPlanesFromMatrix(ref matrix, out near, out far, out left, out right, out top, out bottom);
        }

        static void GetPlanesFromMatrix(ref Matrix matrix, out Plane near, out Plane far, out Plane left, out Plane right, out Plane top, out Plane bottom)
        {
            //http://www.chadvernon.com/blog/resources/directx9/frustum-culling/

            // Left plane
            left.Normal.X = matrix.M14 + matrix.M11;
            left.Normal.Y = matrix.M24 + matrix.M21;
            left.Normal.Z = matrix.M34 + matrix.M31;
            left.D = matrix.M44 + matrix.M41;
            left.Normalize();

            // Right plane
            right.Normal.X = matrix.M14 - matrix.M11;
            right.Normal.Y = matrix.M24 - matrix.M21;
            right.Normal.Z = matrix.M34 - matrix.M31;
            right.D = matrix.M44 - matrix.M41;
            right.Normalize();

            // Top plane
            top.Normal.X = matrix.M14 - matrix.M12;
            top.Normal.Y = matrix.M24 - matrix.M22;
            top.Normal.Z = matrix.M34 - matrix.M32;
            top.D = matrix.M44 - matrix.M42;
            top.Normalize();

            // Bottom plane
            bottom.Normal.X = matrix.M14 + matrix.M12;
            bottom.Normal.Y = matrix.M24 + matrix.M22;
            bottom.Normal.Z = matrix.M34 + matrix.M32;
            bottom.D = matrix.M44 + matrix.M42;
            bottom.Normalize();

            // Near plane
            near.Normal.X = matrix.M13;
            near.Normal.Y = matrix.M23;
            near.Normal.Z = matrix.M33;
            near.D = matrix.M43;
            near.Normalize();

            // Far plane
            far.Normal.X = matrix.M14 - matrix.M13;
            far.Normal.Y = matrix.M24 - matrix.M23;
            far.Normal.Z = matrix.M34 - matrix.M33;
            far.D = matrix.M44 - matrix.M43;
            far.Normalize();
        }

        static Vector3 Get3PlanesInterPoint(ref Plane p1, ref Plane p2, ref Plane p3)
        {
            //P = -d1 * N2xN3 / N1.N2xN3 - d2 * N3xN1 / N2.N3xN1 - d3 * N1xN2 / N3.N1xN2 
            Vector3 v =
                -p1.D * Vector3.Cross(p2.Normal, p3.Normal) / Vector3.Dot(p1.Normal, Vector3.Cross(p2.Normal, p3.Normal))
                - p2.D * Vector3.Cross(p3.Normal, p1.Normal) / Vector3.Dot(p2.Normal, Vector3.Cross(p3.Normal, p1.Normal))
                - p3.D * Vector3.Cross(p1.Normal, p2.Normal) / Vector3.Dot(p3.Normal, Vector3.Cross(p1.Normal, p2.Normal));

            return v;
        }

        public Vector3[] GetCorners()
        {
            var corners = new Vector3[CornerCount];
            corners[0] = Get3PlanesInterPoint(ref near, ref  bottom, ref  right);    //Near1
            corners[1] = Get3PlanesInterPoint(ref near, ref  top, ref  right);       //Near2
            corners[2] = Get3PlanesInterPoint(ref near, ref  top, ref  left);        //Near3
            corners[3] = Get3PlanesInterPoint(ref near, ref  bottom, ref  left);     //Near3
            corners[4] = Get3PlanesInterPoint(ref far, ref  bottom, ref  right);    //Far1
            corners[5] = Get3PlanesInterPoint(ref far, ref  top, ref  right);       //Far2
            corners[6] = Get3PlanesInterPoint(ref far, ref  top, ref  left);        //Far3
            corners[7] = Get3PlanesInterPoint(ref far, ref  bottom, ref  left);     //Far3
            return corners;
        }

        public Plane GetPlane(int index)
        {
            if (index < 0 || 5 < index) throw new ArgumentOutOfRangeException("index");

            switch (index)
            {
                case 0: return left;
                case 1: return right;
                case 2: return top;
                case 3: return bottom;
                case 4: return near;
                case 5: return far;
                default: throw new InvalidOperationException();
            }
        }

        public ContainmentType Contains(ref Vector3 point)
        {
            var result = PlaneIntersectionType.Front;
            var planeResult = PlaneIntersectionType.Front;
            for (int i = 0; i < 6; i++)
            {
                switch (i)
                {
                    case 0: planeResult = near.Intersects(ref point); break;
                    case 1: planeResult = far.Intersects(ref point); break;
                    case 2: planeResult = left.Intersects(ref point); break;
                    case 3: planeResult = right.Intersects(ref point); break;
                    case 4: planeResult = top.Intersects(ref point); break;
                    case 5: planeResult = bottom.Intersects(ref point); break;
                }
                switch (planeResult)
                {
                    case PlaneIntersectionType.Back:
                        return ContainmentType.Disjoint;
                    case PlaneIntersectionType.Intersecting:
                        result = PlaneIntersectionType.Intersecting;
                        break;
                }
            }
            switch (result)
            {
                case PlaneIntersectionType.Intersecting: return ContainmentType.Intersects;
                default: return ContainmentType.Contains;
            }
        }

        public ContainmentType Contains(Vector3 point)
        {
            return Contains(ref point);
        }

        public ContainmentType Contains(Vector3[] points)
        {
            var containsAny = false;
            var containsAll = true;
            for (int i = 0; i < points.Length; i++)
            {
                switch (Contains(ref points[i]))
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
                    return ContainmentType.Contains;
                else
                    return ContainmentType.Intersects;
            }
            else
                return ContainmentType.Disjoint;
        }

        public void Contains(Vector3[] points, out ContainmentType result)
        {
            result = Contains(points);
        }

        void GetBoxToPlanePVertexNVertex(ref BoundingBox box, ref Vector3 planeNormal, out Vector3 p, out Vector3 n)
        {
            p = box.Min;
            if (planeNormal.X >= 0)
                p.X = box.Max.X;
            if (planeNormal.Y >= 0)
                p.Y = box.Max.Y;
            if (planeNormal.Z >= 0)
                p.Z = box.Max.Z;

            n = box.Max;
            if (planeNormal.X >= 0)
                n.X = box.Min.X;
            if (planeNormal.Y >= 0)
                n.Y = box.Min.Y;
            if (planeNormal.Z >= 0)
                n.Z = box.Min.Z;
        }

        public ContainmentType Contains(ref BoundingBox box)
        {
            Vector3 p, n;
            Plane plane;
            var result = ContainmentType.Contains;
            for (int i = 0; i < 6; i++)
            {
                plane = GetPlane(i);
                GetBoxToPlanePVertexNVertex(ref box, ref plane.Normal, out p, out n);
                if (Collision.PlaneIntersectsPoint(ref plane, ref p) == PlaneIntersectionType.Back)
                    return ContainmentType.Disjoint;

                if (Collision.PlaneIntersectsPoint(ref plane, ref n) == PlaneIntersectionType.Back)
                    result = ContainmentType.Intersects;
            }
            return result;
        }

        public void Contains(ref BoundingBox box, out ContainmentType result)
        {
            result = Contains(box.GetCorners());
        }
        
        public ContainmentType Contains(ref BoundingSphere sphere)
        {
            var result = PlaneIntersectionType.Front;
            var planeResult = PlaneIntersectionType.Front;
            for (int i = 0; i < 6; i++)
            {
                switch (i)
                {
                    case 0: planeResult = near.Intersects(ref sphere); break;
                    case 1: planeResult = far.Intersects(ref sphere); break;
                    case 2: planeResult = left.Intersects(ref sphere); break;
                    case 3: planeResult = right.Intersects(ref sphere); break;
                    case 4: planeResult = top.Intersects(ref sphere); break;
                    case 5: planeResult = bottom.Intersects(ref sphere); break;
                }
                switch (planeResult)
                {
                    case PlaneIntersectionType.Back:
                        return ContainmentType.Disjoint;
                    case PlaneIntersectionType.Intersecting:
                        result = PlaneIntersectionType.Intersecting;
                        break;
                }
            }
            switch (result)
            {
                case PlaneIntersectionType.Intersecting: return ContainmentType.Intersects;
                default: return ContainmentType.Contains;
            }
        }

        public void Contains(ref BoundingSphere sphere, out ContainmentType result)
        {
            result = Contains(ref sphere);
        }
        
        public bool Contains(ref BoundingFrustum frustum)
        {
            return Contains(frustum.GetCorners()) != ContainmentType.Disjoint;
        }
        
        public void Contains(ref BoundingFrustum frustum, out bool result)
        {
            result = Contains(frustum.GetCorners()) != ContainmentType.Disjoint;
        }

        public bool Intersects(ref BoundingSphere sphere)
        {
            return Contains(ref sphere) != ContainmentType.Disjoint;
        }
        
        public void Intersects(ref BoundingSphere sphere, out bool result)
        {
            result = Contains(ref sphere) != ContainmentType.Disjoint;
        }
        
        public bool Intersects(ref BoundingBox box)
        {
            return Contains(ref box) != ContainmentType.Disjoint;
        }
        
        public void Intersects(ref BoundingBox box, out bool result)
        {
            result = Contains(ref box) != ContainmentType.Disjoint;
        }

        PlaneIntersectionType PlaneIntersectsPoints(ref Plane plane, Vector3[] points)
        {
            var result = Collision.PlaneIntersectsPoint(ref plane, ref points[0]);
            for (int i = 1; i < points.Length; i++)
                if (Collision.PlaneIntersectsPoint(ref plane, ref points[i]) != result)
                    return PlaneIntersectionType.Intersecting;
            return result;
        }

        public PlaneIntersectionType Intersects(ref Plane plane)
        {
            return PlaneIntersectsPoints(ref plane, GetCorners());
        }

        public void Intersects(ref Plane plane, out PlaneIntersectionType result)
        {
            result = PlaneIntersectsPoints(ref plane, GetCorners());
        }

        public bool Intersects(ref Ray ray)
        {
            float? inDist, outDist;
            return Intersects(ref ray, out inDist, out outDist);
        }
        
        public bool Intersects(ref Ray ray, out float? inDistance, out float? outDistance)
        {
            if (Contains(ray.Position) != ContainmentType.Disjoint)
            {
                float nearstPlaneDistance = float.MaxValue;
                for (int i = 0; i < 6; i++)
                {
                    var plane = GetPlane(i);
                    float distance;
                    if (Collision.RayIntersectsPlane(ref ray, ref plane, out distance) && distance < nearstPlaneDistance)
                    {
                        nearstPlaneDistance = distance;
                    }
                }

                inDistance = nearstPlaneDistance;
                outDistance = null;
                return true;
            }
            else
            {
                //We will find the two points at which the ray enters and exists the frustum
                //These two points make a line which center inside the frustum if the ray intersets it
                //Or outside the frustum if the ray intersects frustum planes outside it.
                float minDist = float.MaxValue;
                float maxDist = float.MinValue;
                for (int i = 0; i < 6; i++)
                {
                    var plane = GetPlane(i);
                    float distance;
                    if (Collision.RayIntersectsPlane(ref ray, ref plane, out distance))
                    {
                        minDist = Math.Min(minDist, distance);
                        maxDist = Math.Max(maxDist, distance);
                    }
                }

                Vector3 minPoint = ray.Position + ray.Direction * minDist;
                Vector3 maxPoint = ray.Position + ray.Direction * maxDist;
                Vector3 center = (minPoint + maxPoint) / 2f;
                if (Contains(ref center) != ContainmentType.Disjoint)
                {
                    inDistance = minDist;
                    outDistance = maxDist;
                    return true;
                }
                else
                {
                    inDistance = null;
                    outDistance = null;
                    return false;
                }
            }
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
