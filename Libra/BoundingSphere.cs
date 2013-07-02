#region Using

using System;
using System.Collections.Generic;

#endregion

// SharpDX.BoundingSphere から移植。
// 一部インタフェースを XNA 形式へ変更。
// 一部ロジックを変更。

namespace Libra
{
    [Serializable]
    public struct BoundingSphere : IEquatable<BoundingSphere>
    {
        public Vector3 Center;

        public float Radius;

        public BoundingSphere(Vector3 center, float radius)
        {
            this.Center = center;
            this.Radius = radius;
        }

        public ContainmentType Contains(BoundingBox box)
        {
            ContainmentType result;
            Contains(ref box, out result);
            return result;
        }

        public void Contains(ref BoundingBox box, out ContainmentType result)
        {
            // SharpDX.Collision.SphereContainsBox より。

            bool boxIntersectsSphere;
            box.Intersects(ref this, out boxIntersectsSphere);
            if (!boxIntersectsSphere)
            {
                result = ContainmentType.Disjoint;
                return;
            }

            Vector3 vector;
            float radiusSquared = Radius * Radius;

            vector.X = Center.X - box.Min.X;
            vector.Y = Center.Y - box.Max.Y;
            vector.Z = Center.Z - box.Max.Z;
            if (radiusSquared < vector.LengthSquared())
            {
                result = ContainmentType.Intersects;
                return;
            }

            vector.X = Center.X - box.Max.X;
            vector.Y = Center.Y - box.Max.Y;
            vector.Z = Center.Z - box.Max.Z;
            if (radiusSquared < vector.LengthSquared())
            {
                result = ContainmentType.Intersects;
                return;
            }

            vector.X = Center.X - box.Max.X;
            vector.Y = Center.Y - box.Min.Y;
            vector.Z = Center.Z - box.Max.Z;
            if (radiusSquared < vector.LengthSquared())
            {
                result = ContainmentType.Intersects;
                return;
            }

            vector.X = Center.X - box.Min.X;
            vector.Y = Center.Y - box.Min.Y;
            vector.Z = Center.Z - box.Max.Z;
            if (radiusSquared < vector.LengthSquared())
            {
                result = ContainmentType.Intersects;
                return;
            }

            vector.X = Center.X - box.Min.X;
            vector.Y = Center.Y - box.Max.Y;
            vector.Z = Center.Z - box.Min.Z;
            if (radiusSquared < vector.LengthSquared())
            {
                result = ContainmentType.Intersects;
                return;
            }

            vector.X = Center.X - box.Max.X;
            vector.Y = Center.Y - box.Max.Y;
            vector.Z = Center.Z - box.Min.Z;
            if (radiusSquared < vector.LengthSquared())
            {
                result = ContainmentType.Intersects;
                return;
            }

            vector.X = Center.X - box.Max.X;
            vector.Y = Center.Y - box.Min.Y;
            vector.Z = Center.Z - box.Min.Z;
            if (radiusSquared < vector.LengthSquared())
            {
                result = ContainmentType.Intersects;
                return;
            }

            vector.X = Center.X - box.Min.X;
            vector.Y = Center.Y - box.Min.Y;
            vector.Z = Center.Z - box.Min.Z;
            if (radiusSquared < vector.LengthSquared())
            {
                result = ContainmentType.Intersects;
                return;
            }

            result = ContainmentType.Contains;
        }

        public ContainmentType Contains(BoundingSphere sphere)
        {
            ContainmentType result;
            Contains(ref sphere, out result);
            return result;
        }

        public void Contains(ref BoundingSphere sphere, out ContainmentType result)
        {
            // SharpDX.Collision.SphereContainsSphere より。

            float distance;
            Vector3.Distance(ref Center, ref sphere.Center, out distance);

            if (Radius + sphere.Radius < distance)
            {
                result = ContainmentType.Disjoint;
                return;
            }

            if (Radius - sphere.Radius < distance)
            {
                result = ContainmentType.Intersects;
                return;
            }

            result = ContainmentType.Contains;
        }

        public ContainmentType Contains(Vector3 point)
        {
            ContainmentType result;
            Contains(ref point, out result);
            return result;
        }

        public void Contains(ref Vector3 point, out ContainmentType result)
        {
            // SharpDX.Collision.SphereContainsPoint より。

            float distanceSquared;
            Vector3.DistanceSquared(ref point, ref Center, out distanceSquared);

            if (distanceSquared <= Radius * Radius)
            {
                result = ContainmentType.Contains;
                return;
            }

            result = ContainmentType.Disjoint;
        }

        public static BoundingSphere CreateFromBoundingBox(BoundingBox box)
        {
            BoundingSphere result;
            CreateFromBoundingBox(ref box, out result);
            return result;
        }

        public static void CreateFromBoundingBox(ref BoundingBox box, out BoundingSphere result)
        {
            result.Center.X = (box.Min.X + box.Max.X) * 0.5f;
            result.Center.Y = (box.Min.Y + box.Max.Y) * 0.5f;
            result.Center.Z = (box.Min.Z + box.Max.Z) * 0.5f;

            Vector3.Distance(ref result.Center, ref box.Max, out result.Radius);
        }

        public static BoundingSphere CreateFromFrustum(BoundingFrustum frustum)
        {
            if (frustum == null) throw new ArgumentNullException("frustum");

            return CreateFromPoints(frustum.GetCorners());
        }

        public static BoundingSphere CreateFromPoints(Vector3[] points)
        {
            BoundingSphere result;
            CreateFromPoints(points, out result);
            return result;
        }

        public static void CreateFromPoints(Vector3[] points, out BoundingSphere result)
        {
            if (points == null) throw new ArgumentNullException("points");

            Vector3 center = Vector3.Zero;
            for (int i = 0; i < points.Length; ++i)
            {
                Vector3.Add(ref points[i], ref center, out center);
            }

            center /= (float) points.Length;

            float radius = 0f;
            for (int i = 0; i < points.Length; ++i)
            {
                float distance;
                Vector3.DistanceSquared(ref center, ref points[i], out distance);

                if (distance > radius)
                    radius = distance;
            }

            radius = (float) Math.Sqrt(radius);

            result.Center = center;
            result.Radius = radius;
        }

        public static void CreateMerged(ref BoundingSphere original, ref BoundingSphere additional, out BoundingSphere result)
        {
            Vector3 difference;
            Vector3.Subtract(ref additional.Center, ref original.Center, out difference);

            float length = difference.Length();
            float radius = original.Radius;
            float radius2 = additional.Radius;

            if (radius + radius2 >= length)
            {
                if (radius - radius2 >= length)
                {
                    result = original;
                    return;
                }

                if (radius2 - radius >= length)
                {
                    result = additional;
                    return;
                }
            }

            Vector3 vector;
            Vector3.Divide(ref difference, length, out vector);

            float min = Math.Min(-radius, length - radius2);
            float max = (Math.Max(radius, length + radius2) - min) * 0.5f;

            float minPlusMax = max + min;
            Vector3.Multiply(ref vector, minPlusMax, out vector);

            Vector3.Add(ref original.Center, ref vector, out result.Center);
            result.Radius = max;
        }

        public static BoundingSphere CreateMerged(BoundingSphere original, BoundingSphere additional)
        {
            BoundingSphere result;
            CreateMerged(ref original, ref additional, out result);
            return result;
        }

        public bool Intersects(BoundingBox box)
        {
            bool result;
            Intersects(ref box, out result);
            return result;
        }

        public void Intersects(ref BoundingBox box, out bool result)
        {
            box.Intersects(ref this, out result);
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
            // SharpDX.Collision.SphereIntersectsSphere より。

            float radiusSum = Radius + sphere.Radius;
            float distanceSquared;
            Vector3.DistanceSquared(ref Center, ref sphere.Center, out distanceSquared);
            result = distanceSquared <= radiusSum * radiusSum;
        }

        public PlaneIntersectionType Intersects(Plane plane)
        {
            PlaneIntersectionType result;
            Intersects(ref plane, out result);
            return result;
        }

        public void Intersects(ref Plane plane, out PlaneIntersectionType result)
        {
            plane.Intersects(ref this, out result);
        }

        public float? Intersects(Ray ray)
        {
            float? result;
            Intersects(ref ray, out result);
            return result;
        }

        public void Intersects(ref Ray ray, out float? result)
        {
            ray.Intersects(ref this, out result);
        }

        public BoundingSphere Transform(Matrix matrix)
        {
            BoundingSphere result;
            Transform(ref matrix, out result);
            return result;
        }

        public void Transform(ref Matrix matrix, out BoundingSphere result)
        {
            // MonoGame より。

            var m1 = matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12 + matrix.M13 * matrix.M13;
            var m2 = matrix.M21 * matrix.M21 + matrix.M22 * matrix.M22 + matrix.M23 * matrix.M23;
            var m3 = matrix.M31 * matrix.M31 + matrix.M32 * matrix.M32 + matrix.M33 * matrix.M33;
            var max = Math.Max(m1, Math.Max(m2, m3));

            Vector3.Transform(ref Center, ref matrix, out result.Center);
            result.Radius = Radius * (float) Math.Sqrt(max);
        }

        #region IEquatable

        public static bool operator ==(BoundingSphere left, BoundingSphere right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoundingSphere left, BoundingSphere right)
        {
            return !left.Equals(right);
        }

        public bool Equals(BoundingSphere value)
        {
            return Center == value.Center && Radius == value.Radius;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((BoundingSphere) obj);
        }

        public override int GetHashCode()
        {
            return Center.GetHashCode() ^ Radius.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{Center:" + Center + " Radius:" + Radius + "}";
        }

        #endregion
    }
}
