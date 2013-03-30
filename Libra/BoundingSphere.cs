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

        public bool Intersects(ref Ray ray)
        {
            float distance;
            return Collision.RayIntersectsSphere(ref ray, ref this, out distance);
        }

        public bool Intersects(ref Ray ray, out float distance)
        {
            return Collision.RayIntersectsSphere(ref ray, ref this, out distance);
        }

        public bool Intersects(ref Ray ray, out Vector3 point)
        {
            return Collision.RayIntersectsSphere(ref ray, ref this, out point);
        }

        public PlaneIntersectionType Intersects(ref Plane plane)
        {
            return Collision.PlaneIntersectsSphere(ref plane, ref this);
        }

        public bool Intersects(ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3)
        {
            return Collision.SphereIntersectsTriangle(ref this, ref vertex1, ref vertex2, ref vertex3);
        }

        public bool Intersects(ref BoundingBox box)
        {
            return Collision.BoxIntersectsSphere(ref box, ref this);
        }

        public bool Intersects(ref BoundingSphere sphere)
        {
            return Collision.SphereIntersectsSphere(ref this, ref sphere);
        }

        public ContainmentType Contains(ref Vector3 point)
        {
            return Collision.SphereContainsPoint(ref this, ref point);
        }

        public ContainmentType Contains(ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3)
        {
            return Collision.SphereContainsTriangle(ref this, ref vertex1, ref vertex2, ref vertex3);
        }

        public ContainmentType Contains(ref BoundingBox box)
        {
            return Collision.SphereContainsBox(ref this, ref box);
        }

        public ContainmentType Contains(ref BoundingSphere sphere)
        {
            return Collision.SphereContainsSphere(ref this, ref sphere);
        }

        public static void FromPoints(Vector3[] points, out BoundingSphere result)
        {
            //Find the center of all points.
            Vector3 center = Vector3.Zero;
            for (int i = 0; i < points.Length; ++i)
            {
                Vector3.Add(ref points[i], ref center, out center);
            }

            //This is the center of our sphere.
            center /= (float) points.Length;

            //Find the radius of the sphere
            float radius = 0f;
            for (int i = 0; i < points.Length; ++i)
            {
                //We are doing a relative distance comparasin to find the maximum distance
                //from the center of our sphere.
                float distance;
                Vector3.DistanceSquared(ref center, ref points[i], out distance);

                if (distance > radius)
                    radius = distance;
            }

            //Find the real distance from the DistanceSquared.
            radius = (float) Math.Sqrt(radius);

            //Construct the sphere.
            result.Center = center;
            result.Radius = radius;
        }

        public static BoundingSphere FromPoints(Vector3[] points)
        {
            BoundingSphere result;
            FromPoints(points, out result);
            return result;
        }

        public static void FromBox(ref BoundingBox box, out BoundingSphere result)
        {
            Vector3.Lerp(ref box.Min, ref box.Max, 0.5f, out result.Center);

            float x = box.Min.X - box.Max.X;
            float y = box.Min.Y - box.Max.Y;
            float z = box.Min.Z - box.Max.Z;

            float distance = (float) (Math.Sqrt((x * x) + (y * y) + (z * z)));
            result.Radius = distance * 0.5f;
        }

        public static BoundingSphere FromBox(BoundingBox box)
        {
            BoundingSphere result;
            FromBox(ref box, out result);
            return result;
        }

        public static void Merge(ref BoundingSphere value1, ref BoundingSphere value2, out BoundingSphere result)
        {
            Vector3 difference = value2.Center - value1.Center;

            float length = difference.Length();
            float radius = value1.Radius;
            float radius2 = value2.Radius;

            if (radius + radius2 >= length)
            {
                if (radius - radius2 >= length)
                {
                    result = value1;
                    return;
                }

                if (radius2 - radius >= length)
                {
                    result = value2;
                    return;
                }
            }

            Vector3 vector = difference * (1.0f / length);
            float min = Math.Min(-radius, length - radius2);
            float max = (Math.Max(radius, length + radius2) - min) * 0.5f;

            result.Center = value1.Center + vector * (max + min);
            result.Radius = max;
        }

        public static BoundingSphere Merge(BoundingSphere value1, BoundingSphere value2)
        {
            BoundingSphere result;
            Merge(ref value1, ref value2, out result);
            return result;
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
