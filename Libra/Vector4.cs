#region Using

using System;
using System.Runtime.InteropServices;

#endregion

// SharpDX.Vector4 から移植。
// 一部インタフェースを XNA 形式へ変更。
// 一部ロジックを変更。

namespace Libra
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector4 : IEquatable<Vector4>
    {
        public static readonly Vector4 Zero = new Vector4();

        public static readonly Vector4 One = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        public static readonly Vector4 UnitX = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);

        public static readonly Vector4 UnitY = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);

        public static readonly Vector4 UnitZ = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);

        public static readonly Vector4 UnitW = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

        public float X;
        
        public float Y;
        
        public float Z;
        
        public float W;

        public Vector4(float value)
        {
            X = value;
            Y = value;
            Z = value;
            W = value;
        }

        public Vector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4(Vector2 value, float z, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = z;
            W = w;
        }

        public Vector4(Vector3 value, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
            W = w;
        }

        public static Vector4 Add(Vector4 left, Vector4 right)
        {
            Vector4 result;
            Add(ref left, ref right, out result);
            return result;
        }

        public static void Add(ref Vector4 left, ref Vector4 right, out Vector4 result)
        {
            result = new Vector4(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
        }

        public static Vector4 Subtract(Vector4 left, Vector4 right)
        {
            Vector4 result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        public static void Subtract(ref Vector4 left, ref Vector4 right, out Vector4 result)
        {
            result = new Vector4(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
        }

        public static Vector4 Multiply(Vector4 value, float scale)
        {
            Vector4 result;
            Multiply(ref value, scale, out result);
            return result;
        }

        public static void Multiply(ref Vector4 value, float scale, out Vector4 result)
        {
            result = new Vector4(value.X * scale, value.Y * scale, value.Z * scale, value.W * scale);
        }

        public static Vector4 Multiply(Vector4 left, Vector4 right)
        {
            Vector4 result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        public static void Multiply(ref Vector4 left, ref Vector4 right, out Vector4 result)
        {
            result = new Vector4(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);
        }

        public static Vector4 Divide(Vector4 value, float scale)
        {
            Vector4 result;
            Divide(ref value, scale, out result);
            return result;
        }

        public static void Divide(ref Vector4 value, float scale, out Vector4 result)
        {
            result = new Vector4(value.X / scale, value.Y / scale, value.Z / scale, value.W / scale);
        }

        public static Vector4 Divide(Vector4 left, Vector4 right)
        {
            Vector4 result;
            Divide(ref left, ref right, out result);
            return result;
        }

        public static void Divide(ref Vector4 left, ref Vector4 right, out Vector4 result)
        {
            result = new Vector4(left.X / right.X, left.Y / right.Y, left.Z / right.Z, left.W / right.W);
        }

        public static Vector4 Negate(Vector4 value)
        {
            Vector4 result;
            Negate(ref value, out result);
            return result;
        }

        public static void Negate(ref Vector4 value, out Vector4 result)
        {
            result = new Vector4(-value.X, -value.Y, -value.Z, -value.W);
        }

        public static Vector4 Barycentric(Vector4 value1, Vector4 value2, Vector4 value3, float amount1, float amount2)
        {
            Vector4 result;
            Barycentric(ref value1, ref value2, ref value3, amount1, amount2, out result);
            return result;
        }

        public static void Barycentric(ref Vector4 value1, ref Vector4 value2, ref Vector4 value3,
                                       float amount1, float amount2, out Vector4 result)
        {
            result = new Vector4((value1.X + (amount1 * (value2.X - value1.X))) + (amount2 * (value3.X - value1.X)),
                (value1.Y + (amount1 * (value2.Y - value1.Y))) + (amount2 * (value3.Y - value1.Y)),
                (value1.Z + (amount1 * (value2.Z - value1.Z))) + (amount2 * (value3.Z - value1.Z)),
                (value1.W + (amount1 * (value2.W - value1.W))) + (amount2 * (value3.W - value1.W)));
        }

        public static Vector4 Clamp(Vector4 value, Vector4 min, Vector4 max)
        {
            Vector4 result;
            Clamp(ref value, ref min, ref max, out result);
            return result;
        }

        public static void Clamp(ref Vector4 value, ref Vector4 min, ref Vector4 max, out Vector4 result)
        {
            result = new Vector4(
                MathHelper.Clamp(value.X, min.X, max.X),
                MathHelper.Clamp(value.Y, min.Y, max.Y),
                MathHelper.Clamp(value.Z, min.Z, max.Z),
                MathHelper.Clamp(value.W, min.W, max.W));
        }

        public static float Distance(Vector4 value1, Vector4 value2)
        {
            float result;
            Distance(ref value1, ref value2, out result);
            return result;
        }

        public static void Distance(ref Vector4 value1, ref Vector4 value2, out float result)
        {
            DistanceSquared(ref value1, ref value2, out result);
            result = (float) Math.Sqrt(result);
        }

        public static float DistanceSquared(Vector4 value1, Vector4 value2)
        {
            float result;
            DistanceSquared(ref value1, ref value2, out result);
            return result;
        }

        public static void DistanceSquared(ref Vector4 value1, ref Vector4 value2, out float result)
        {
            float x = value1.X - value2.X;
            float y = value1.Y - value2.Y;
            float z = value1.Z - value2.Z;
            float w = value1.W - value2.W;

            result = (x * x) + (y * y) + (z * z) + (w * w);
        }

        public static float Dot(Vector4 left, Vector4 right)
        {
            float result;
            Dot(ref left, ref right, out result);
            return result;
        }

        public static void Dot(ref Vector4 left, ref Vector4 right, out float result)
        {
            result = (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z) + (left.W * right.W);
        }

        public static Vector4 Normalize(Vector4 value)
        {
            Normalize(ref value, out value);
            return value;
        }

        public static void Normalize(ref Vector4 value, out Vector4 result)
        {
            float length = value.Length();

            result = value;
            if (float.Epsilon < length)
            {
                var inverse = 1 / length;
                result.X *= inverse;
                result.Y *= inverse;
                result.Z *= inverse;
                result.W *= inverse;
            }
        }

        public static Vector4 Lerp(Vector4 start, Vector4 end, float amount)
        {
            Vector4 result;
            Lerp(ref start, ref end, amount, out result);
            return result;
        }

        public static void Lerp(ref Vector4 start, ref Vector4 end, float amount, out Vector4 result)
        {
            result.X = start.X + ((end.X - start.X) * amount);
            result.Y = start.Y + ((end.Y - start.Y) * amount);
            result.Z = start.Z + ((end.Z - start.Z) * amount);
            result.W = start.W + ((end.W - start.W) * amount);
        }

        public static Vector4 SmoothStep(Vector4 start, Vector4 end, float amount)
        {
            Vector4 result;
            SmoothStep(ref start, ref end, amount, out result);
            return result;
        }

        public static void SmoothStep(ref Vector4 start, ref Vector4 end, float amount, out Vector4 result)
        {
            amount = (amount > 1.0f) ? 1.0f : ((amount < 0.0f) ? 0.0f : amount);
            amount = (amount * amount) * (3.0f - (2.0f * amount));

            result.X = start.X + ((end.X - start.X) * amount);
            result.Y = start.Y + ((end.Y - start.Y) * amount);
            result.Z = start.Z + ((end.Z - start.Z) * amount);
            result.W = start.W + ((end.W - start.W) * amount);
        }

        public static Vector4 Hermite(Vector4 value1, Vector4 tangent1, Vector4 value2, Vector4 tangent2, float amount)
        {
            Vector4 result;
            Hermite(ref value1, ref tangent1, ref value2, ref tangent2, amount, out result);
            return result;
        }

        public static void Hermite(ref Vector4 value1, ref Vector4 tangent1,
                                   ref Vector4 value2, ref Vector4 tangent2,
                                   float amount, out Vector4 result)
        {
            float squared = amount * amount;
            float cubed = amount * squared;
            float part1 = ((2.0f * cubed) - (3.0f * squared)) + 1.0f;
            float part2 = (-2.0f * cubed) + (3.0f * squared);
            float part3 = (cubed - (2.0f * squared)) + amount;
            float part4 = cubed - squared;

            result = new Vector4((((value1.X * part1) + (value2.X * part2)) + (tangent1.X * part3)) + (tangent2.X * part4),
                (((value1.Y * part1) + (value2.Y * part2)) + (tangent1.Y * part3)) + (tangent2.Y * part4),
                (((value1.Z * part1) + (value2.Z * part2)) + (tangent1.Z * part3)) + (tangent2.Z * part4),
                (((value1.W * part1) + (value2.W * part2)) + (tangent1.W * part3)) + (tangent2.W * part4));
        }

        public static Vector4 CatmullRom(Vector4 value1, Vector4 value2, Vector4 value3, Vector4 value4, float amount)
        {
            Vector4 result;
            CatmullRom(ref value1, ref value2, ref value3, ref value4, amount, out result);
            return result;
        }

        public static void CatmullRom(ref Vector4 value1, ref Vector4 value2,
                                      ref Vector4 value3, ref Vector4 value4,
                                      float amount, out Vector4 result)
        {
            float squared = amount * amount;
            float cubed = amount * squared;

            result.X = 0.5f * ((((2.0f * value2.X) + ((-value1.X + value3.X) * amount)) +
                (((((2.0f * value1.X) - (5.0f * value2.X)) + (4.0f * value3.X)) - value4.X) * squared)) +
                ((((-value1.X + (3.0f * value2.X)) - (3.0f * value3.X)) + value4.X) * cubed));
            result.Y = 0.5f * ((((2.0f * value2.Y) + ((-value1.Y + value3.Y) * amount)) +
                (((((2.0f * value1.Y) - (5.0f * value2.Y)) + (4.0f * value3.Y)) - value4.Y) * squared)) +
                ((((-value1.Y + (3.0f * value2.Y)) - (3.0f * value3.Y)) + value4.Y) * cubed));
            result.Z = 0.5f * ((((2.0f * value2.Z) + ((-value1.Z + value3.Z) * amount)) +
                (((((2.0f * value1.Z) - (5.0f * value2.Z)) + (4.0f * value3.Z)) - value4.Z) * squared)) +
                ((((-value1.Z + (3.0f * value2.Z)) - (3.0f * value3.Z)) + value4.Z) * cubed));
            result.W = 0.5f * ((((2.0f * value2.W) + ((-value1.W + value3.W) * amount)) +
                (((((2.0f * value1.W) - (5.0f * value2.W)) + (4.0f * value3.W)) - value4.W) * squared)) +
                ((((-value1.W + (3.0f * value2.W)) - (3.0f * value3.W)) + value4.W) * cubed));
        }

        public static Vector4 Max(Vector4 left, Vector4 right)
        {
            Vector4 result;
            Max(ref left, ref right, out result);
            return result;
        }

        public static void Max(ref Vector4 left, ref Vector4 right, out Vector4 result)
        {
            result.X = (left.X > right.X) ? left.X : right.X;
            result.Y = (left.Y > right.Y) ? left.Y : right.Y;
            result.Z = (left.Z > right.Z) ? left.Z : right.Z;
            result.W = (left.W > right.W) ? left.W : right.W;
        }

        public static Vector4 Min(Vector4 left, Vector4 right)
        {
            Vector4 result;
            Min(ref left, ref right, out result);
            return result;
        }

        public static void Min(ref Vector4 left, ref Vector4 right, out Vector4 result)
        {
            result.X = (left.X < right.X) ? left.X : right.X;
            result.Y = (left.Y < right.Y) ? left.Y : right.Y;
            result.Z = (left.Z < right.Z) ? left.Z : right.Z;
            result.W = (left.W < right.W) ? left.W : right.W;
        }

        public static Vector4 Transform(Vector4 vector, Quaternion rotation)
        {
            Vector4 result;
            Transform(ref vector, ref rotation, out result);
            return result;
        }

        public static void Transform(ref Vector4 vector, ref Quaternion rotation, out Vector4 result)
        {
            float x = rotation.X + rotation.X;
            float y = rotation.Y + rotation.Y;
            float z = rotation.Z + rotation.Z;
            float wx = rotation.W * x;
            float wy = rotation.W * y;
            float wz = rotation.W * z;
            float xx = rotation.X * x;
            float xy = rotation.X * y;
            float xz = rotation.X * z;
            float yy = rotation.Y * y;
            float yz = rotation.Y * z;
            float zz = rotation.Z * z;

            result = new Vector4(
                ((vector.X * ((1.0f - yy) - zz)) + (vector.Y * (xy - wz))) + (vector.Z * (xz + wy)),
                ((vector.X * (xy + wz)) + (vector.Y * ((1.0f - xx) - zz))) + (vector.Z * (yz - wx)),
                ((vector.X * (xz - wy)) + (vector.Y * (yz + wx))) + (vector.Z * ((1.0f - xx) - yy)),
                vector.W);
        }

        public static void Transform(Vector4[] source, ref Quaternion rotation, Vector4[] destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            if (destination.Length < source.Length) throw new ArgumentOutOfRangeException("destination");

            for (int i = 0; i < source.Length; ++i)
            {
                Transform(ref source[i], ref rotation, out destination[i]);
            }
        }

        public static Vector4 Transform(Vector4 vector, Matrix transform)
        {
            Vector4 result;
            Transform(ref vector, ref transform, out result);
            return result;
        }

        public static void Transform(ref Vector4 vector, ref Matrix transform, out Vector4 result)
        {
            result = new Vector4(
                (vector.X * transform.M11) + (vector.Y * transform.M21) + (vector.Z * transform.M31) + (vector.W * transform.M41),
                (vector.X * transform.M12) + (vector.Y * transform.M22) + (vector.Z * transform.M32) + (vector.W * transform.M42),
                (vector.X * transform.M13) + (vector.Y * transform.M23) + (vector.Z * transform.M33) + (vector.W * transform.M43),
                (vector.X * transform.M14) + (vector.Y * transform.M24) + (vector.Z * transform.M34) + (vector.W * transform.M44));
        }

        public static void Transform(Vector4[] source, ref Matrix transform, Vector4[] destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            if (destination.Length < source.Length) throw new ArgumentOutOfRangeException("destination");

            for (int i = 0; i < source.Length; ++i)
            {
                Transform(ref source[i], ref transform, out destination[i]);
            }
        }

        #region operator

        public static Vector4 operator -(Vector4 value)
        {
            return new Vector4(-value.X, -value.Y, -value.Z, -value.W);
        }

        public static Vector4 operator +(Vector4 left, Vector4 right)
        {
            Vector4 result;
            Add(ref left, ref right, out result);
            return result;
        }

        public static Vector4 operator -(Vector4 left, Vector4 right)
        {
            Vector4 result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        public static Vector4 operator *(float scale, Vector4 value)
        {
            Vector4 result;
            Multiply(ref value, scale, out result);
            return result;
        }

        public static Vector4 operator *(Vector4 value, float scale)
        {
            Vector4 result;
            Multiply(ref value, scale, out result);
            return result;
        }

        public static Vector4 operator *(Vector4 left, Vector4 right)
        {
            Vector4 result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        public static Vector4 operator /(Vector4 value, float scale)
        {
            Vector4 result;
            Divide(ref value, scale, out result);
            return result;
        }

        public static Vector4 operator /(Vector4 left, Vector4 right)
        {
            Vector4 result;
            Divide(ref left, ref right, out result);
            return result;
        }

        #endregion

        public float Length()
        {
            return (float) Math.Sqrt(LengthSquared());
        }

        public float LengthSquared()
        {
            return (X * X) + (Y * Y) + (Z * Z) + (W * W);
        }

        public void Normalize()
        {
            Normalize(ref this, out this);
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        #region IEquatable

        public static bool operator ==(Vector4 left, Vector4 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector4 left, Vector4 right)
        {
            return !left.Equals(right);
        }

        public bool Equals(Vector4 other)
        {
            return ((float) Math.Abs(other.X - X) < MathHelper.ZeroTolerance &&
                (float) Math.Abs(other.Y - Y) < MathHelper.ZeroTolerance &&
                (float) Math.Abs(other.Z - Z) < MathHelper.ZeroTolerance &&
                (float) Math.Abs(other.W - W) < MathHelper.ZeroTolerance);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((Vector4) obj);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{X:" + X + " Y:" + Y + " Z:" + Z + " W:" + W + "}";
        }

        #endregion
    }
}
