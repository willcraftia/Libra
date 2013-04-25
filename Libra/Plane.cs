#region Using

using System;

#endregion

// SharpDX.Plane から移植。
// 一部インタフェースを XNA 形式へ変更。
// 一部ロジックを変更。

namespace Libra
{
    [Serializable]
    public struct Plane : IEquatable<Plane>
    {
        public Vector3 Normal;

        public float D;

        public Plane(float a, float b, float c, float d)
        {
            Normal.X = a;
            Normal.Y = b;
            Normal.Z = c;
            D = d;
        }

        public Plane(Vector3 normal, float d)
        {
            Normal = normal;
            D = d;
        }

        // 法線が normal、点 point を含む平面。
        public Plane(Vector3 normal, Vector3 point)
        {
            Normal = normal;

            Vector3.Dot(ref normal, ref point, out D);
            D = -D;
        }

        public Plane(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            float x1 = point2.X - point1.X;
            float y1 = point2.Y - point1.Y;
            float z1 = point2.Z - point1.Z;
            float x2 = point3.X - point1.X;
            float y2 = point3.Y - point1.Y;
            float z2 = point3.Z - point1.Z;
            float yz = (y1 * z2) - (z1 * y2);
            float xz = (z1 * x2) - (x1 * z2);
            float xy = (x1 * y2) - (y1 * x2);
            float invPyth = 1.0f / (float) (Math.Sqrt((yz * yz) + (xz * xz) + (xy * xy)));

            Normal.X = yz * invPyth;
            Normal.Y = xz * invPyth;
            Normal.Z = xy * invPyth;
            D = -((Normal.X * point1.X) + (Normal.Y * point1.Y) + (Normal.Z * point1.Z));
        }

        public float Dot(Vector4 value)
        {
            float result;
            Dot(ref value, out result);
            return result;
        }

        public void Dot(ref Vector4 value, out float result)
        {
            result = Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z + D * value.W;
        }

        public float DotCoordinate(Vector3 value)
        {
            float result;
            DotCoordinate(ref value, out result);
            return result;
        }

        public void DotCoordinate(ref Vector3 value, out float result)
        {
            result = Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z + D;
        }

        public float DotNormal(Vector3 value)
        {
            float result;
            DotNormal(ref value, out result);
            return result;
        }

        public void DotNormal(ref Vector3 value, out float result)
        {
            result = Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z;
        }

        public PlaneIntersectionType Intersects(BoundingBox box)
        {
            PlaneIntersectionType result;
            Intersects(ref box, out result);
            return result;
        }

        public void Intersects(ref BoundingBox box, out PlaneIntersectionType result)
        {
            // SharpDX.Collision.PlaneIntersectsBox より。

            Vector3 min;
            Vector3 max;

            max.X = (Normal.X >= 0.0f) ? box.Min.X : box.Max.X;
            max.Y = (Normal.Y >= 0.0f) ? box.Min.Y : box.Max.Y;
            max.Z = (Normal.Z >= 0.0f) ? box.Min.Z : box.Max.Z;
            min.X = (Normal.X >= 0.0f) ? box.Max.X : box.Min.X;
            min.Y = (Normal.Y >= 0.0f) ? box.Max.Y : box.Min.Y;
            min.Z = (Normal.Z >= 0.0f) ? box.Max.Z : box.Min.Z;

            float distance;
            Vector3.Dot(ref Normal, ref max, out distance);

            if (distance + D > 0.0f)
            {
                result = PlaneIntersectionType.Front;
                return;
            }

            distance = Vector3.Dot(Normal, min);

            if (distance + D < 0.0f)
            {
                result = PlaneIntersectionType.Back;
                return;
            }

            result = PlaneIntersectionType.Intersecting;
        }

        // TODO
        //
        // public PlaneIntersectionType Intersects(BoundingFrustum frustum) 未実装。

        public PlaneIntersectionType Intersects(BoundingSphere sphere)
        {
            PlaneIntersectionType result;
            Intersects(ref sphere, out result);
            return result;
        }

        public void Intersects(ref BoundingSphere sphere, out PlaneIntersectionType result)
        {
            // SharpDX.Collision.PlaneIntersectsSphere より。

            float distance;
            Vector3.Dot(ref Normal, ref sphere.Center, out distance);
            distance += D;

            if (distance > sphere.Radius)
            {
                result = PlaneIntersectionType.Front;
                return;
            }

            if (distance < -sphere.Radius)
            {
                result = PlaneIntersectionType.Back;
                return;
            }

            result = PlaneIntersectionType.Intersecting;
        }

        public void Normalize()
        {
            Normalize(ref this, out this);
        }

        public static Plane Normalize(Plane plane)
        {
            Plane result;
            Normalize(ref plane, out result);
            return result;
        }

        public static void Normalize(ref Plane plane, out Plane result)
        {
            float factor = 1.0f / (float) Math.Sqrt(
                plane.Normal.X * plane.Normal.X + plane.Normal.Y * plane.Normal.Y + plane.Normal.Z * plane.Normal.Z);

            result.Normal.X = plane.Normal.X * factor;
            result.Normal.Y = plane.Normal.Y * factor;
            result.Normal.Z = plane.Normal.Z * factor;
            result.D = plane.D * factor;
        }

        public static Plane Transform(Plane plane, Matrix matrix)
        {
            Plane result;
            Transform(ref plane, ref matrix, out result);
            return result;
        }

        public static void Transform(ref Plane plane, ref Matrix matrix, out Plane result)
        {
            float x = plane.Normal.X;
            float y = plane.Normal.Y;
            float z = plane.Normal.Z;
            float d = plane.D;

            Matrix inverse;
            Matrix.Invert(ref matrix, out inverse);

            result.Normal.X = x * inverse.M11 + y * inverse.M12 + z * inverse.M13 + d * inverse.M14;
            result.Normal.Y = x * inverse.M21 + y * inverse.M22 + z * inverse.M23 + d * inverse.M24;
            result.Normal.Z = x * inverse.M31 + y * inverse.M32 + z * inverse.M33 + d * inverse.M34;
            result.D = x * inverse.M41 + y * inverse.M42 + z * inverse.M43 + d * inverse.M44;
        }

        public static Plane Transform(Plane plane, Quaternion rotation)
        {
            Plane result;
            Transform(ref plane, ref rotation, out result);
            return result;
        }

        public static void Transform(ref Plane plane, ref Quaternion rotation, out Plane result)
        {
            float x2 = rotation.X + rotation.X;
            float y2 = rotation.Y + rotation.Y;
            float z2 = rotation.Z + rotation.Z;
            float wx = rotation.W * x2;
            float wy = rotation.W * y2;
            float wz = rotation.W * z2;
            float xx = rotation.X * x2;
            float xy = rotation.X * y2;
            float xz = rotation.X * z2;
            float yy = rotation.Y * y2;
            float yz = rotation.Y * z2;
            float zz = rotation.Z * z2;

            float x = plane.Normal.X;
            float y = plane.Normal.Y;
            float z = plane.Normal.Z;

            result.Normal.X = ((x * ((1.0f - yy) - zz)) + (y * (xy - wz))) + (z * (xz + wy));
            result.Normal.Y = ((x * (xy + wz)) + (y * ((1.0f - xx) - zz))) + (z * (yz - wx));
            result.Normal.Z = ((x * (xz - wy)) + (y * (yz + wx))) + (z * ((1.0f - xx) - yy));
            result.D = plane.D;
        }

        #region operator

        public static Plane operator *(float scale, Plane plane)
        {
            return new Plane(plane.Normal.X * scale, plane.Normal.Y * scale, plane.Normal.Z * scale, plane.D * scale);
        }

        public static Plane operator *(Plane plane, float scale)
        {
            return new Plane(plane.Normal.X * scale, plane.Normal.Y * scale, plane.Normal.Z * scale, plane.D * scale);
        }

        #endregion

        #region IEquatable

        public static bool operator ==(Plane left, Plane right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Plane left, Plane right)
        {
            return !left.Equals(right);
        }

        public bool Equals(Plane value)
        {
            return Normal == value.Normal && D == value.D;
        }

        public override bool Equals(object value)
        {
            if (value == null)
                return false;

            if (!ReferenceEquals(value.GetType(), typeof(Plane)))
                return false;

            return Equals((Plane) value);
        }

        public override int GetHashCode()
        {
            return Normal.GetHashCode() + D.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{Normal:" + Normal + " D:" + D + "}";
        }

        #endregion
    }
}
