#region Using

using System;
using System.Runtime.InteropServices;

#endregion

// SharpDX.Vector2 から移植。
// 一部インタフェースを XNA 形式へ変更。
// 一部ロジックを変更。

namespace Libra
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2 : IEquatable<Vector2>
    {
        public static readonly Vector2 Zero = new Vector2();

        public static readonly Vector2 One = new Vector2(1.0f);

        public static readonly Vector2 UnitX = new Vector2(1.0f, 0.0f);

        public static readonly Vector2 UnitY = new Vector2(0.0f, 1.0f);

        public float X;
        
        public float Y;

        public Vector2(float value)
        {
            X = value;
            Y = value;
        }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 Add(Vector2 value1, Vector2 value2)
        {
            Vector2 result;
            Add(ref value1, ref value2, out result);
            return result;
        }

        public static void Add(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result = new Vector2(value1.X + value2.X, value1.Y + value2.Y);
        }

        public static Vector2 Subtract(Vector2 value1, Vector2 value2)
        {
            Vector2 result;
            Subtract(ref value1, ref value2, out result);
            return result;
        }

        public static void Subtract(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result = new Vector2(value1.X - value2.X, value1.Y - value2.Y);
        }

        public static Vector2 Multiply(Vector2 value, float scale)
        {
            Vector2 result;
            Multiply(ref value, scale, out result);
            return result;
        }

        public static Vector2 Multiply(Vector2 value1, Vector2 value2)
        {
            Vector2 result;
            Multiply(ref value1, ref value2, out result);
            return result;
        }

        public static void Multiply(ref Vector2 value, float scale, out Vector2 result)
        {
            result = new Vector2(value.X * scale, value.Y * scale);
        }

        public static void Multiply(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result = new Vector2(value1.X * value2.X, value1.Y * value2.Y);
        }

        public static Vector2 Divide(Vector2 value, float scale)
        {
            Vector2 result;
            Divide(ref value, scale, out result);
            return result;
        }

        public static Vector2 Divide(Vector2 value1, Vector2 value2)
        {
            Vector2 result;
            Divide(ref value1, ref value2, out result);
            return result;
        }

        public static void Divide(ref Vector2 value, float scale, out Vector2 result)
        {
            result = new Vector2(value.X / scale, value.Y / scale);
        }

        public static void Divide(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result = new Vector2(value1.X / value2.X, value1.Y / value2.Y);
        }

        public static Vector2 Negate(Vector2 value)
        {
            Vector2 result;
            Negate(ref value, out result);
            return result;
        }

        public static void Negate(ref Vector2 value, out Vector2 result)
        {
            result = new Vector2(-value.X, -value.Y);
        }

        public static Vector2 Barycentric(Vector2 value1, Vector2 value2, Vector2 value3, float amount1, float amount2)
        {
            Vector2 result;
            Barycentric(ref value1, ref value2, ref value3, amount1, amount2, out result);
            return result;
        }

        public static void Barycentric(ref Vector2 value1, ref Vector2 value2, ref Vector2 value3,
                                       float amount1, float amount2, out Vector2 result)
        {
            result = new Vector2((value1.X + (amount1 * (value2.X - value1.X))) + (amount2 * (value3.X - value1.X)),
                (value1.Y + (amount1 * (value2.Y - value1.Y))) + (amount2 * (value3.Y - value1.Y)));
        }

        public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max)
        {
            Vector2 result;
            Clamp(ref value, ref min, ref max, out result);
            return result;
        }

        public static void Clamp(ref Vector2 value, ref Vector2 min, ref Vector2 max, out Vector2 result)
        {
            result = new Vector2(
                MathHelper.Clamp(value.X, min.X, max.X),
                MathHelper.Clamp(value.Y, min.Y, max.Y));
        }

        public static float Distance(Vector2 value1, Vector2 value2)
        {
            float result;
            Distance(ref value1, ref value2, out result);
            return result;
        }


        public static void Distance(ref Vector2 value1, ref Vector2 value2, out float result)
        {
            DistanceSquared(ref value1, ref value2, out result);
            result = (float) Math.Sqrt(result);
        }

        public static float DistanceSquared(Vector2 value1, Vector2 value2)
        {
            float result;
            DistanceSquared(ref value1, ref value2, out result);
            return result;
        }

        public static void DistanceSquared(ref Vector2 value1, ref Vector2 value2, out float result)
        {
            float x = value1.X - value2.X;
            float y = value1.Y - value2.Y;

            result = (x * x) + (y * y);
        }

        public static float Dot(Vector2 value1, Vector2 value2)
        {
            float result;
            Dot(ref value1, ref value2, out result);
            return result;
        }

        public static void Dot(ref Vector2 value1, ref Vector2 value2, out float result)
        {
            result = value1.X * value2.X + value1.Y * value2.Y;
        }

        public static Vector2 Normalize(Vector2 value)
        {
            Vector2 result;
            Normalize(ref value, out result);
            return result;
        }

        public static void Normalize(ref Vector2 value, out Vector2 result)
        {
            float length = value.Length();

            result = value;
            if (float.Epsilon < length)
            {
                var inverse = 1 / length;
                result.X *= inverse;
                result.Y *= inverse;
            }
        }

        public static Vector2 Lerp(Vector2 start, Vector2 end, float amount)
        {
            Vector2 result;
            Lerp(ref start, ref end, amount, out result);
            return result;
        }

        public static void Lerp(ref Vector2 start, ref Vector2 end, float amount, out Vector2 result)
        {
            result = new Vector2(
                MathHelper.Lerp(start.X, end.X, amount),
                MathHelper.Lerp(start.Y, end.Y, amount));
        }

        public static Vector2 SmoothStep(Vector2 start, Vector2 end, float amount)
        {
            Vector2 result;
            SmoothStep(ref start, ref end, amount, out result);
            return result;
        }

        public static void SmoothStep(ref Vector2 start, ref Vector2 end, float amount, out Vector2 result)
        {
            amount = (amount > 1.0f) ? 1.0f : ((amount < 0.0f) ? 0.0f : amount);
            amount = (amount * amount) * (3.0f - (2.0f * amount));

            result.X = start.X + ((end.X - start.X) * amount);
            result.Y = start.Y + ((end.Y - start.Y) * amount);
        }

        public static Vector2 Hermite(Vector2 value1, Vector2 tangent1,
                                      Vector2 value2, Vector2 tangent2,
                                      float amount)
        {
            Vector2 result;
            Hermite(ref value1, ref tangent1, ref value2, ref tangent2, amount, out result);
            return result;
        }

        public static void Hermite(ref Vector2 value1, ref Vector2 tangent1,
                                   ref Vector2 value2, ref Vector2 tangent2,
                                   float amount, out Vector2 result)
        {
            float squared = amount * amount;
            float cubed = amount * squared;
            float part1 = ((2.0f * cubed) - (3.0f * squared)) + 1.0f;
            float part2 = (-2.0f * cubed) + (3.0f * squared);
            float part3 = (cubed - (2.0f * squared)) + amount;
            float part4 = cubed - squared;

            result.X = (((value1.X * part1) + (value2.X * part2)) + (tangent1.X * part3)) + (tangent2.X * part4);
            result.Y = (((value1.Y * part1) + (value2.Y * part2)) + (tangent1.Y * part3)) + (tangent2.Y * part4);
        }

        public static Vector2 CatmullRom(Vector2 value1, Vector2 value2,
                                         Vector2 value3, Vector2 value4,
                                         float amount)
        {
            Vector2 result;
            CatmullRom(ref value1, ref value2, ref value3, ref value4, amount, out result);
            return result;
        }

        public static void CatmullRom(ref Vector2 value1, ref Vector2 value2,
                                      ref Vector2 value3, ref Vector2 value4,
                                      float amount, out Vector2 result)
        {
            float squared = amount * amount;
            float cubed = amount * squared;

            result.X = 0.5f * ((((2.0f * value2.X) + ((-value1.X + value3.X) * amount)) +
                (((((2.0f * value1.X) - (5.0f * value2.X)) + (4.0f * value3.X)) - value4.X) * squared)) +
                ((((-value1.X + (3.0f * value2.X)) - (3.0f * value3.X)) + value4.X) * cubed));

            result.Y = 0.5f * ((((2.0f * value2.Y) + ((-value1.Y + value3.Y) * amount)) +
                (((((2.0f * value1.Y) - (5.0f * value2.Y)) + (4.0f * value3.Y)) - value4.Y) * squared)) +
                ((((-value1.Y + (3.0f * value2.Y)) - (3.0f * value3.Y)) + value4.Y) * cubed));
        }

        public static Vector2 Max(Vector2 value1, Vector2 value2)
        {
            Vector2 result;
            Max(ref value1, ref value2, out result);
            return result;
        }

        public static void Max(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result = new Vector2(
                MathHelper.Max(value1.X, value2.X),
                MathHelper.Max(value1.Y, value2.Y));
        }

        public static Vector2 Min(Vector2 value1, Vector2 value2)
        {
            Vector2 result;
            Min(ref value1, ref value2, out result);
            return result;
        }

        public static void Min(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result = new Vector2(
                MathHelper.Min(value1.X, value2.X),
                MathHelper.Min(value1.Y, value2.Y));
        }

        public static Vector2 Reflect(Vector2 vector, Vector2 normal)
        {
            Vector2 result;
            Reflect(ref vector, ref normal, out result);
            return result;
        }

        public static void Reflect(ref Vector2 vector, ref Vector2 normal, out Vector2 result)
        {
            float dot;
            Dot(ref vector, ref normal, out dot);
            result.X = vector.X - ((2f * dot) * normal.X);
            result.Y = vector.Y - ((2f * dot) * normal.Y);
        }

        public static void Transform(ref Vector2 vector, ref Quaternion rotation, out Vector2 result)
        {
            float x = rotation.X + rotation.X;
            float y = rotation.Y + rotation.Y;
            float z = rotation.Z + rotation.Z;
            float wz = rotation.W * z;
            float xx = rotation.X * x;
            float xy = rotation.X * y;
            float yy = rotation.Y * y;
            float zz = rotation.Z * z;

            result = new Vector2(
                (vector.X * (1.0f - yy - zz)) + (vector.Y * (xy - wz)),
                (vector.X * (xy + wz)) + (vector.Y * (1.0f - xx - zz)));
        }

        public static Vector2 Transform(Vector2 vector, Quaternion rotation)
        {
            Vector2 result;
            Transform(ref vector, ref rotation, out result);
            return result;
        }

        public static void Transform(Vector2[] source, ref Quaternion rotation, Vector2[] destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            if (destination.Length < source.Length) throw new ArgumentOutOfRangeException("destination");

            for (int i = 0; i < source.Length; ++i)
            {
                Transform(ref source[i], ref rotation, out destination[i]);
            }
        }

        public static Vector2 Transform(Vector2 position, Matrix matrix)
        {
            Vector2 result;
            Transform(ref position, ref matrix, out result);
            return result;
        }

        public static void Transform(ref Vector2 position, ref Matrix matrix, out Vector2 result)
        {
            result = new Vector2(
                (position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M41,
                (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M42);
        }

        public static void Transform(Vector2[] source, ref Matrix transform, Vector2[] destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            if (destination.Length < source.Length) throw new ArgumentOutOfRangeException("destination");

            for (int i = 0; i < source.Length; ++i)
            {
                Transform(ref source[i], ref transform, out destination[i]);
            }
        }

        public static Vector2 TransformNormal(Vector2 normal, Matrix transform)
        {
            Vector2 result;
            TransformNormal(ref normal, ref transform, out result);
            return result;
        }

        public static void TransformNormal(ref Vector2 normal, ref Matrix transform, out Vector2 result)
        {
            result = new Vector2(
                (normal.X * transform.M11) + (normal.Y * transform.M21),
                (normal.X * transform.M12) + (normal.Y * transform.M22));
        }

        public static void TransformNormal(Vector2[] source, ref Matrix transform, Vector2[] destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            if (destination.Length < source.Length) throw new ArgumentOutOfRangeException("destination");

            for (int i = 0; i < source.Length; ++i)
            {
                TransformNormal(ref source[i], ref transform, out destination[i]);
            }
        }

        #region operator

        public static Vector2 operator -(Vector2 value)
        {
            return new Vector2(-value.X, -value.Y);
        }

        public static Vector2 operator +(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X + right.X, left.Y + right.Y);
        }

        public static Vector2 operator *(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X * right.X, left.Y * right.Y);
        }

        public static Vector2 operator -(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X - right.X, left.Y - right.Y);
        }

        public static Vector2 operator *(float scale, Vector2 value)
        {
            return new Vector2(value.X * scale, value.Y * scale);
        }

        public static Vector2 operator *(Vector2 value, float scale)
        {
            return new Vector2(value.X * scale, value.Y * scale);
        }

        public static Vector2 operator /(Vector2 value, float scale)
        {
            return new Vector2(value.X / scale, value.Y / scale);
        }

        #endregion

        public float Length()
        {
            return (float) Math.Sqrt(LengthSquared());
        }

        public float LengthSquared()
        {
            return X * X + Y * Y;
        }

        public void Normalize()
        {
            Normalize(ref this, out this);
        }

        public Vector4 ToVector4()
        {
            return new Vector4(X, Y, 0, 0);
        }

        #region IEquatable

        public static bool operator ==(Vector2 left, Vector2 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector2 left, Vector2 right)
        {
            return !left.Equals(right);
        }

        public bool Equals(Vector2 other)
        {
            return ((float) Math.Abs(other.X - X) < MathHelper.ZeroTolerance &&
                (float) Math.Abs(other.Y - Y) < MathHelper.ZeroTolerance);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((Vector2) obj);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{X:" + X + " Y:" + Y + "}";
        }

        #endregion
    }
}
