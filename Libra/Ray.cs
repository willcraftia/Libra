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

        public float? Intersects(BoundingBox box)
        {
            float? result;
            Intersects(ref box, out result);
            return result;
        }

        public void Intersects(ref BoundingBox box, out float? result)
        {
            // SharpDX.Collision.RayIntersectsBox より。

            float distance = 0;
            float tmax = float.MaxValue;

            if (Math.Abs(Direction.X) < MathHelper.ZeroTolerance)
            {
                if (Position.X < box.Min.X || box.Max.X < Position.X)
                {
                    result = null;
                    return;
                }
            }
            else
            {
                float inverse = 1.0f / Direction.X;
                float t1 = (box.Min.X - Position.X) * inverse;
                float t2 = (box.Max.X - Position.X) * inverse;

                if (t2 < t1)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                distance = Math.Max(t1, distance);
                tmax = Math.Min(t2, tmax);

                if (tmax < distance)
                {
                    result = null;
                    return;
                }
            }

            if (Math.Abs(Direction.Y) < MathHelper.ZeroTolerance)
            {
                if (Position.Y < box.Min.Y || box.Max.Y < Position.Y)
                {
                    result = null;
                    return;
                }
            }
            else
            {
                float inverse = 1.0f / Direction.Y;
                float t1 = (box.Min.Y - Position.Y) * inverse;
                float t2 = (box.Max.Y - Position.Y) * inverse;

                if (t2 < t1)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                distance = Math.Max(t1, distance);
                tmax = Math.Min(t2, tmax);

                if (tmax < distance)
                {
                    result = null;
                    return;
                }
            }

            if (Math.Abs(Direction.Z) < MathHelper.ZeroTolerance)
            {
                if (Position.Z < box.Min.Z || box.Max.Z < Position.Z)
                {
                    result = null;
                    return;
                }
            }
            else
            {
                float inverse = 1.0f / Direction.Z;
                float t1 = (box.Min.Z - Position.Z) * inverse;
                float t2 = (box.Max.Z - Position.Z) * inverse;

                if (t2 < t1)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                distance = Math.Max(t1, distance);
                tmax = Math.Min(t2, tmax);

                if (tmax < distance)
                {
                    result = null;
                    return;
                }
            }

            result = distance;
        }

        public float? Intersects(BoundingFrustum frustum)
        {
            float? result;
            frustum.Intersects(ref this, out result);
            return result;
        }

        public float? Intersects(BoundingSphere sphere)
        {
            float? result;
            Intersects(ref sphere, out result);
            return result;
        }

        public void Intersects(ref BoundingSphere sphere, out float? result)
        {
            // SharpDX.Collision.RayIntersectsSphere より。

            Vector3 m;
            Vector3.Subtract(ref Position, ref sphere.Center, out m);

            float b = Vector3.Dot(m, Direction);
            float c = Vector3.Dot(m, m) - sphere.Radius * sphere.Radius;

            if (c > 0f && b > 0f)
            {
                result = null;
                return;
            }

            float discriminant = b * b - c;

            if (discriminant < 0f)
            {
                result = null;
                return;
            }

            result = -b - (float) Math.Sqrt(discriminant);

            if (result < 0f)
                result = null;
        }

        public float? Intersects(Plane plane)
        {
            float? result;
            Intersects(ref plane, out result);
            return result;
        }

        public void Intersects(ref Plane plane, out float? result)
        {
            // SharpDX.Collision.RayIntersectsPlane より。

            float direction;
            Vector3.Dot(ref plane.Normal, ref Direction, out direction);

            if (Math.Abs(direction) < MathHelper.ZeroTolerance)
            {
                result = null;
                return;
            }

            float position;
            Vector3.Dot(ref plane.Normal, ref Position, out position);
            float distance = (-plane.D - position) / direction;

            if (distance < 0f)
            {
                if (distance < -MathHelper.ZeroTolerance)
                {
                    result = null;
                    return;
                }

                result = 0;
            }
            else
            {
                result = distance;
            }
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
