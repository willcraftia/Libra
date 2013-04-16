#region Using

using System;
using System.Collections.Generic;

#endregion

// SharpDX.BoundingBox から移植。
// 一部インタフェースを XNA 形式へ変更。
// 一部ロジックを変更。

namespace Libra
{
    [Serializable]
    public struct BoundingBox : IEquatable<BoundingBox>
    {
        public const int CornerCount = 8;

        public static readonly BoundingBox Empty = new BoundingBox(new Vector3(float.MaxValue), new Vector3(float.MinValue));

        public Vector3 Min;

        public Vector3 Max;

        public BoundingBox(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public ContainmentType Contains(BoundingBox box)
        {
            ContainmentType result;
            Contains(ref box, out result);
            return result;
        }

        public void Contains(ref BoundingBox box, out ContainmentType result)
        {
            if (box.Max.X < Min.X || Max.X < box.Min.X ||
                box.Max.Y < Min.Y || Max.Y < box.Min.Y ||
                box.Max.Z < Min.Z || Max.Z < box.Min.Z)
            {
                result = ContainmentType.Disjoint;
            }
            else if (
                Min.X <= box.Min.X && box.Max.X <= Max.X &&
                Min.Y <= box.Min.Y && box.Max.Y <= Max.Y &&
                Min.Z <= box.Min.Z && box.Max.Z <= Max.Z)
            {
                result = ContainmentType.Contains;
            }
            else
            {
                result = ContainmentType.Intersects;
            }
        }

        // TODO
        //
        // public ContainmentType Contains(BoundingFrustum frustum) 未実装
        // しかし、境界ボックスが境界錐台を含むかどうかを検査する状況が思い浮かばないため
        // (その逆は境界錐台カリングで必要とするが)、
        // 実装の必要が無いとも考えている。

        public ContainmentType Contains(BoundingSphere sphere)
        {
            ContainmentType result;
            Contains(ref sphere, out result);
            return result;
        }

        public void Contains(ref BoundingSphere sphere, out ContainmentType result)
        {
            // ShaprdDX.Collision.BoxContainsSphere より。

            Vector3 vector;
            Vector3.Clamp(ref sphere.Center, ref Min, ref Max, out vector);

            float distance;
            Vector3.DistanceSquared(ref sphere.Center, ref vector, out distance);

            if (sphere.Radius * sphere.Radius < distance)
            {
                result = ContainmentType.Disjoint;
                return;
            }

            if ((Min.X + sphere.Radius <= sphere.Center.X) && (sphere.Center.X <= Max.X - sphere.Radius) && (Max.X - Min.X > sphere.Radius) &&
                (Min.Y + sphere.Radius <= sphere.Center.Y) && (sphere.Center.Y <= Max.Y - sphere.Radius) && (Max.Y - Min.Y > sphere.Radius) &&
                (Min.Z + sphere.Radius <= sphere.Center.Z) && (sphere.Center.Z <= Max.Z - sphere.Radius) && (Max.X - Min.X > sphere.Radius))
            {
                result = ContainmentType.Contains;
                return;
            }

            result = ContainmentType.Intersects;
        }

        public ContainmentType Contains(Vector3 point)
        {
            ContainmentType result;
            Contains(ref point, out result);
            return result;
        }

        public void Contains(ref Vector3 point, out ContainmentType result)
        {
            if (point.X < Min.X || Max.X < point.X ||
                point.Y < Min.Y || Max.Y < point.Y ||
                point.Z < Min.Z || Max.Z < point.Z)
            {
                result = ContainmentType.Disjoint;
            }
            else if (
                point.X == Min.X || point.X == Max.X ||
                point.Y == Min.Y || point.Y == Max.Y ||
                point.Z == Min.Z || point.Z == Max.Z)
            {
                result = ContainmentType.Intersects;
            }
            else
            {
                result = ContainmentType.Contains;
            }
        }

        public static BoundingBox CreateFromPoints(IEnumerable<Vector3> points)
        {
            BoundingBox result;
            CreateFromPoints(points, out result);
            return result;
        }

        public static void CreateFromPoints(IEnumerable<Vector3> points, out BoundingBox result)
        {
            if (points == null) throw new ArgumentNullException("points");

            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);

            foreach (var point in points)
            {
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }

            result = new BoundingBox(min, max);
        }

        public static BoundingBox CreateFromSphere(BoundingSphere sphere)
        {
            BoundingBox result;
            CreateFromSphere(ref sphere, out result);
            return result;
        }

        public static void CreateFromSphere(ref BoundingSphere sphere, out BoundingBox result)
        {
            result.Min = new Vector3(
                sphere.Center.X - sphere.Radius,
                sphere.Center.Y - sphere.Radius,
                sphere.Center.Z - sphere.Radius);
            
            result.Max = new Vector3(
                sphere.Center.X + sphere.Radius,
                sphere.Center.Y + sphere.Radius,
                sphere.Center.Z + sphere.Radius);
        }

        public static BoundingBox CreateMerged(BoundingBox original, BoundingBox additional)
        {
            BoundingBox result;
            CreateMerged(ref original, ref additional, out result);
            return result;
        }

        public static void CreateMerged(ref BoundingBox original, ref BoundingBox additional, out BoundingBox result)
        {
            Vector3.Min(ref original.Min, ref additional.Min, out result.Min);
            Vector3.Max(ref original.Max, ref additional.Max, out result.Max);
        }

        public Vector3[] GetCorners()
        {
            var results = new Vector3[CornerCount];
            GetCorners(results);
            return results;
        }

        public void GetCorners(Vector3[] results)
        {
            if (results == null) throw new ArgumentNullException("result");
            if (results.Length < CornerCount) throw new ArgumentOutOfRangeException("result");

            results[0] = new Vector3(Min.X, Max.Y, Max.Z);
            results[1] = new Vector3(Max.X, Max.Y, Max.Z);
            results[2] = new Vector3(Max.X, Min.Y, Max.Z);
            results[3] = new Vector3(Min.X, Min.Y, Max.Z);
            results[4] = new Vector3(Min.X, Max.Y, Min.Z);
            results[5] = new Vector3(Max.X, Max.Y, Min.Z);
            results[6] = new Vector3(Max.X, Min.Y, Min.Z);
            results[7] = new Vector3(Min.X, Min.Y, Min.Z);
        }

        public bool Intersects(BoundingBox box)
        {
            bool result;
            Intersects(ref box, out result);
            return result;
        }

        public void Intersects(ref BoundingBox box, out bool result)
        {
            // SharpDX.Collision.BoxIntersectsBox より。

            if (Min.X > box.Max.X || box.Min.X > Max.X)
            {
                result = false;
                return;
            }

            if (Min.Y > box.Max.Y || box.Min.Y > Max.Y)
            {
                result = false;
                return;
            }

            if (Min.Z > box.Max.Z || box.Min.Z > Max.Z)
            {
                result = false;
                return;
            }

            result = true;
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
            // SharpDX.Collision.BoxIntersectsSphere より。

            Vector3 vector;
            Vector3.Clamp(ref sphere.Center, ref Min, ref Max, out vector);

            float distance;
            Vector3.DistanceSquared(ref sphere.Center, ref vector, out distance);

            result = distance <= sphere.Radius * sphere.Radius;
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

        public Vector3 GetCenter()
        {
            Vector3 result;
            GetCenter(out result);
            return result;
        }

        public void GetCenter(out Vector3 result)
        {
            // 参考: result = (box.Max + box.Min) / 2
            Vector3 maxMin;
            Vector3.Add(ref Max, ref Min, out maxMin);
            Vector3.Divide(ref maxMin, 2, out result);
        }

        public Vector3 GetSize()
        {
            Vector3 result;
            GetSize(out result);
            return result;
        }

        public void GetSize(out Vector3 result)
        {
            // 参考: result = box.Max - box.Min
            Vector3.Subtract(ref Max, ref Min, out result);
        }

        public Vector3 GetHalfSize()
        {
            Vector3 result;
            GetHalfSize(out result);
            return result;
        }

        public void GetHalfSize(out Vector3 result)
        {
            // 参考: result = (box.Max - box.Min) / 2
            Vector3 size;
            Vector3.Subtract(ref Max, ref Min, out size);
            Vector3.Divide(ref size, 2, out result);
        }

        #region IEquatable

        public static bool operator ==(BoundingBox left, BoundingBox right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoundingBox left, BoundingBox right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            return Min.GetHashCode() ^ Max.GetHashCode();
        }

        public bool Equals(BoundingBox other)
        {
            return Min == other.Min && Max == other.Max;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((Vector3) obj);
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{Min:" + Min + " Max:" + Max + "}";
        }

        #endregion
    }
}
