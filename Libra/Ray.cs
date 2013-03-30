#region Using

using System;

#endregion

// SharpDX.Ray から移植。
// 一部インタフェースを XNA 形式へ変更。
// 一部ロジックを変更。

namespace Libra
{
    [Serializable]
    public struct Ray : IEquatable<Ray>
    {
        public Vector3 Position;

        public Vector3 Direction;

        public Ray(Vector3 position, Vector3 direction)
        {
            Position = position;
            Direction = direction;
        }

        public bool Intersects(ref Vector3 point)
        {
            return Collision.RayIntersectsPoint(ref this, ref point);
        }

        public bool Intersects(ref Ray ray)
        {
            Vector3 point;
            return Collision.RayIntersectsRay(ref this, ref ray, out point);
        }

        public bool Intersects(ref Ray ray, out Vector3 point)
        {
            return Collision.RayIntersectsRay(ref this, ref ray, out point);
        }

        public bool Intersects(ref Plane plane)
        {
            float distance;
            return Collision.RayIntersectsPlane(ref this, ref plane, out distance);
        }

        public bool Intersects(ref Plane plane, out float distance)
        {
            return Collision.RayIntersectsPlane(ref this, ref plane, out distance);
        }

        public bool Intersects(ref Plane plane, out Vector3 point)
        {
            return Collision.RayIntersectsPlane(ref this, ref plane, out point);
        }

        public bool Intersects(ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3)
        {
            float distance;
            return Collision.RayIntersectsTriangle(ref this, ref vertex1, ref vertex2, ref vertex3, out distance);
        }

        public bool Intersects(ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out float distance)
        {
            return Collision.RayIntersectsTriangle(ref this, ref vertex1, ref vertex2, ref vertex3, out distance);
        }

        public bool Intersects(ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out Vector3 point)
        {
            return Collision.RayIntersectsTriangle(ref this, ref vertex1, ref vertex2, ref vertex3, out point);
        }

        public bool Intersects(ref BoundingBox box)
        {
            float distance;
            return Collision.RayIntersectsBox(ref this, ref box, out distance);
        }

        public bool Intersects(ref BoundingBox box, out float distance)
        {
            return Collision.RayIntersectsBox(ref this, ref box, out distance);
        }

        public bool Intersects(ref BoundingBox box, out Vector3 point)
        {
            return Collision.RayIntersectsBox(ref this, ref box, out point);
        }

        public bool Intersects(ref BoundingSphere sphere)
        {
            float distance;
            return Collision.RayIntersectsSphere(ref this, ref sphere, out distance);
        }

        public bool Intersects(ref BoundingSphere sphere, out float distance)
        {
            return Collision.RayIntersectsSphere(ref this, ref sphere, out distance);
        }

        public bool Intersects(ref BoundingSphere sphere, out Vector3 point)
        {
            return Collision.RayIntersectsSphere(ref this, ref sphere, out point);
        }

        #region IEquatable

        public static bool operator ==(Ray left, Ray right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Ray left, Ray right)
        {
            return !left.Equals(right);
        }

        public bool Equals(Ray other)
        {
            return Position == other.Position && Direction == other.Direction;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((Ray) obj);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Direction.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{Position:" + Position + " Direction:" + Direction + "}";
        }

        #endregion
    }
}
