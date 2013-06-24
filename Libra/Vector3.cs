#region Using

using System;

#endregion

// SharpDX.Vector3 から移植。
// 一部インタフェースを XNA 形式へ変更。
// 一部ロジックを変更。

namespace Libra
{
    public struct Vector3 : IEquatable<Vector3>
    {
        public static readonly Vector3 Zero = new Vector3(0);

        public static readonly Vector3 One = new Vector3(1);

        public static readonly Vector3 UnitX = new Vector3(1, 0, 0);

        public static readonly Vector3 UnitY = new Vector3(0, 1, 0);

        public static readonly Vector3 UnitZ = new Vector3(0, 0, 1);

        public static readonly Vector3 NegativeUnitX = new Vector3(-1, 0, 0);

        public static readonly Vector3 NegativeUnitY = new Vector3(0, -1, 0);

        public static readonly Vector3 NegativeUnitZ = new Vector3(0, 0, -1);

        public static readonly Vector3 Up       = new Vector3(0, 1, 0);

        public static readonly Vector3 Down     = new Vector3(0, -1, 0);

        public static readonly Vector3 Right    = new Vector3(1, 0, 0);

        public static readonly Vector3 Left     = new Vector3(-1, 0, 0);

        public static readonly Vector3 Forward  = new Vector3(0, 0, -1);

        public static readonly Vector3 Backward = new Vector3(0, 0, 1);

        public float X;
        
        public float Y;
        
        public float Z;

        public Vector3(float value)
        {
            X = value;
            Y = value;
            Z = value;
        }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(Vector2 value, float z)
        {
            X = value.X;
            Y = value.Y;
            Z = z;
        }

        public static Vector3 Add(Vector3 value1, Vector3 value2)
        {
            Vector3 result;
            Add(ref value1, ref value2, out result);
            return result;
        }

        public static void Add(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result = new Vector3(value1.X + value2.X, value1.Y + value2.Y, value1.Z + value2.Z);
        }

        public static Vector3 Subtract(Vector3 value1, Vector3 value2)
        {
            Vector3 result;
            Subtract(ref value1, ref value2, out result);
            return result;
        }

        public static void Subtract(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result = new Vector3(value1.X - value2.X, value1.Y - value2.Y, value1.Z - value2.Z);
        }

        public static Vector3 Multiply(Vector3 value, float scale)
        {
            Vector3 result;
            Multiply(ref value, scale, out result);
            return result;
        }

        public static Vector3 Multiply(Vector3 value1, Vector3 value2)
        {
            Vector3 result;
            Multiply(ref value1, ref value2, out result);
            return result;
        }

        public static void Multiply(ref Vector3 value, float scale, out Vector3 result)
        {
            result = new Vector3(value.X * scale, value.Y * scale, value.Z * scale);
        }

        public static void Multiply(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result = new Vector3(value1.X * value2.X, value1.Y * value2.Y, value1.Z * value2.Z);
        }

        public static Vector3 Divide(Vector3 value, float scale)
        {
            Vector3 result;
            Divide(ref value, scale, out result);
            return result;
        }

        public static Vector3 Divide(Vector3 value1, Vector3 value2)
        {
            Vector3 result;
            Divide(ref value1, ref value2, out result);
            return result;
        }

        public static void Divide(ref Vector3 value, float scale, out Vector3 result)
        {
            var inverse = 1 / scale;
            result = new Vector3(value.X * inverse, value.Y * inverse, value.Z * inverse);
        }

        public static void Divide(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result = new Vector3(value1.X / value2.X, value1.Y / value2.Y, value1.Z / value2.Z);
        }

        public static Vector3 Negate(Vector3 value)
        {
            Vector3 result;
            Negate(ref value, out result);
            return result;
        }

        public static void Negate(ref Vector3 value, out Vector3 result)
        {
            result = new Vector3(-value.X, -value.Y, -value.Z);
        }

        public static Vector3 Barycentric(Vector3 value1, Vector3 value2, Vector3 value3, float amount1, float amount2)
        {
            Vector3 result;
            Barycentric(ref value1, ref value2, ref value3, amount1, amount2, out result);
            return result;
        }

        public static void Barycentric(ref Vector3 value1, ref Vector3 value2, ref Vector3 value3,
            float amount1, float amount2, out Vector3 result)
        {
            result = new Vector3(
                MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2),
                MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2),
                MathHelper.Barycentric(value1.Z, value2.Z, value3.Z, amount1, amount2));
        }

        public static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max)
        {
            Vector3 result;
            Clamp(ref value, ref min, ref max, out result);
            return result;
        }

        public static void Clamp(ref Vector3 value, ref Vector3 min, ref Vector3 max, out Vector3 result)
        {
            result = new Vector3(
                MathHelper.Clamp(value.X, min.X, max.X),
                MathHelper.Clamp(value.Y, min.Y, max.Y),
                MathHelper.Clamp(value.Z, min.Z, max.Z));
        }

        public static Vector3 Cross(Vector3 value1, Vector3 value2)
        {
            Vector3 result;
            Cross(ref value1, ref value2, out result);
            return result;
        }

        public static void Cross(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result = new Vector3(
                (value1.Y * value2.Z) - (value1.Z * value2.Y),
                (value1.Z * value2.X) - (value1.X * value2.Z),
                (value1.X * value2.Y) - (value1.Y * value2.X));
        }

        public static float Distance(Vector3 value1, Vector3 value2)
        {
            float result;
            DistanceSquared(ref value1, ref value2, out result);
            return (float) Math.Sqrt(result);
        }

        public static void Distance(ref Vector3 value1, ref Vector3 value2, out float result)
        {
            DistanceSquared(ref value1, ref value2, out result);
            result = (float) Math.Sqrt(result);
        }

        public static float DistanceSquared(Vector3 value1, Vector3 value2)
        {
            float result;
            DistanceSquared(ref value1, ref value2, out result);
            return result;
        }

        public static void DistanceSquared(ref Vector3 value1, ref Vector3 value2, out float result)
        {
            float x = value1.X - value2.X;
            float y = value1.Y - value2.Y;
            float z = value1.Z - value2.Z;

            result = (x * x) + (y * y) + (z * z);
        }

        public static float Dot(Vector3 value1, Vector3 value2)
        {
            float result;
            Dot(ref value1, ref value2, out result);
            return result;
        }

        public static void Dot(ref Vector3 value1, ref Vector3 value2, out float result)
        {
            result = value1.X * value2.X + value1.Y * value2.Y + value1.Z * value2.Z;
        }

        public static Vector3 Normalize(Vector3 value)
        {
            Normalize(ref value, out value);
            return value;
        }

        public static void Normalize(ref Vector3 value, out Vector3 result)
        {
            float length = value.Length();

            result = value;
            if (float.Epsilon < length)
            {
                var inverse = 1 / length;
                result.X *= inverse;
                result.Y *= inverse;
                result.Z *= inverse;
            }
        }

        public static Vector3 Lerp(Vector3 start, Vector3 end, float amount)
        {
            Vector3 result;
            Lerp(ref start, ref end, amount, out result);
            return result;
        }

        public static void Lerp(ref Vector3 start, ref Vector3 end, float amount, out Vector3 result)
        {
            result = new Vector3(
                MathHelper.Lerp(start.X, end.X, amount),
                MathHelper.Lerp(start.Y, end.Y, amount),
                MathHelper.Lerp(start.Z, end.Z, amount));
        }

        public static Vector3 SmoothStep(Vector3 start, Vector3 end, float amount)
        {
            Vector3 result;
            SmoothStep(ref start, ref end, amount, out result);
            return result;
        }

        public static void SmoothStep(ref Vector3 start, ref Vector3 end, float amount, out Vector3 result)
        {
            // MathHelper.SmoothStep の繰り返し呼び出しでは少し非効率。
            // 故に、ここで展開して算出。

            amount = (amount > 1.0f) ? 1.0f : ((amount < 0.0f) ? 0.0f : amount);
            amount = (amount * amount) * (3.0f - (2.0f * amount));

            result.X = start.X + ((end.X - start.X) * amount);
            result.Y = start.Y + ((end.Y - start.Y) * amount);
            result.Z = start.Z + ((end.Z - start.Z) * amount);
        }

        public static Vector3 Hermite(Vector3 value1, Vector3 tangent1,
                                      Vector3 value2, Vector3 tangent2,
                                      float amount)
        {
            Vector3 result;
            Hermite(ref value1, ref tangent1, ref value2, ref tangent2, amount, out result);
            return result;
        }

        public static void Hermite(ref Vector3 value1, ref Vector3 tangent1,
                                   ref Vector3 value2, ref Vector3 tangent2,
                                   float amount, out Vector3 result)
        {
            // MathHelper.Hermite の繰り返し呼び出しでは少し非効率。
            // 故に、ここで展開して算出。

            float squared = amount * amount;
            float cubed = amount * squared;
            float part1 = ((2.0f * cubed) - (3.0f * squared)) + 1.0f;
            float part2 = (-2.0f * cubed) + (3.0f * squared);
            float part3 = (cubed - (2.0f * squared)) + amount;
            float part4 = cubed - squared;

            result.X = (((value1.X * part1) + (value2.X * part2)) + (tangent1.X * part3)) + (tangent2.X * part4);
            result.Y = (((value1.Y * part1) + (value2.Y * part2)) + (tangent1.Y * part3)) + (tangent2.Y * part4);
            result.Z = (((value1.Z * part1) + (value2.Z * part2)) + (tangent1.Z * part3)) + (tangent2.Z * part4);
        }

        public static Vector3 CatmullRom(Vector3 value1, Vector3 value2, Vector3 value3, Vector3 value4, float amount)
        {
            Vector3 result;
            CatmullRom(ref value1, ref value2, ref value3, ref value4, amount, out result);
            return result;
        }

        public static void CatmullRom(ref Vector3 value1, ref Vector3 value2, ref Vector3 value3, ref Vector3 value4,
            float amount, out Vector3 result)
        {
            // MathHelper.CatmullRom の繰り返し呼び出しでは少し非効率。
            // 故に、ここで展開して算出。

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
        }

        public static Vector3 Max(Vector3 value1, Vector3 value2)
        {
            Vector3 result;
            Max(ref value1, ref value2, out result);
            return result;
        }

        public static void Max(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result = new Vector3(
                MathHelper.Max(value1.X, value2.X),
                MathHelper.Max(value1.Y, value2.Y),
                MathHelper.Max(value1.Z, value2.Z));
        }

        public static Vector3 Min(Vector3 value1, Vector3 value2)
        {
            Vector3 result;
            Min(ref value1, ref value2, out result);
            return result;
        }

        public static void Min(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result = new Vector3(
                MathHelper.Min(value1.X, value2.X),
                MathHelper.Min(value1.Y, value2.Y),
                MathHelper.Min(value1.Z, value2.Z));
        }

        public static Vector3 Reflect(Vector3 vector, Vector3 normal)
        {
            Vector3 result;
            Reflect(ref vector, ref normal, out result);
            return result;
        }

        public static void Reflect(ref Vector3 vector, ref Vector3 normal, out Vector3 result)
        {
            float dot;
            Dot(ref vector, ref normal, out dot);

            result.X = vector.X - ((2.0f * dot) * normal.X);
            result.Y = vector.Y - ((2.0f * dot) * normal.Y);
            result.Z = vector.Z - ((2.0f * dot) * normal.Z);
        }

        public static Vector3 Transform(Vector3 vector, Quaternion rotation)
        {
            Vector3 result;
            Transform(ref vector, ref rotation, out result);
            return result;
        }

        public static void Transform(ref Vector3 vector, ref Quaternion rotation, out Vector3 result)
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

            result = new Vector3(
                ((vector.X * ((1.0f - yy) - zz)) + (vector.Y * (xy - wz))) + (vector.Z * (xz + wy)),
                ((vector.X * (xy + wz)) + (vector.Y * ((1.0f - xx) - zz))) + (vector.Z * (yz - wx)),
                ((vector.X * (xz - wy)) + (vector.Y * (yz + wx))) + (vector.Z * ((1.0f - xx) - yy)));
        }

        public static void Transform(Vector3[] source, ref Quaternion rotation, Vector3[] destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            if (destination.Length < source.Length) throw new ArgumentOutOfRangeException("destination");

            for (int i = 0; i < source.Length; ++i)
            {
                Transform(ref source[i], ref rotation, out destination[i]);
            }
        }

        public static Vector3 Transform(Vector3 vector, Matrix matrix)
        {
            Vector3 result;
            Transform(ref vector, ref matrix, out result);
            return result;
        }

        public static void Transform(ref Vector3 vector, ref Matrix matrix, out Vector3 result)
        {
            result = new Vector3(
                (vector.X * matrix.M11) + (vector.Y * matrix.M21) + (vector.Z * matrix.M31) + matrix.M41,
                (vector.X * matrix.M12) + (vector.Y * matrix.M22) + (vector.Z * matrix.M32) + matrix.M42,
                (vector.X * matrix.M13) + (vector.Y * matrix.M23) + (vector.Z * matrix.M33) + matrix.M43);
        }

        public static void Transform(Vector3[] source, ref Matrix matrix, Vector3[] destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            if (destination.Length < source.Length) throw new ArgumentOutOfRangeException("destination");

            for (int i = 0; i < source.Length; ++i)
            {
                Transform(ref source[i], ref matrix, out destination[i]);
            }
        }

        public static Vector3 TransformNormal(Vector3 normal, Matrix matrix)
        {
            Vector3 result;
            TransformNormal(ref normal, ref matrix, out result);
            return result;
        }

        public static void TransformNormal(ref Vector3 normal, ref Matrix matrix, out Vector3 result)
        {
            result = new Vector3(
                (normal.X * matrix.M11) + (normal.Y * matrix.M21) + (normal.Z * matrix.M31),
                (normal.X * matrix.M12) + (normal.Y * matrix.M22) + (normal.Z * matrix.M32),
                (normal.X * matrix.M13) + (normal.Y * matrix.M23) + (normal.Z * matrix.M33));
        }

        public static void TransformNormal(Vector3[] source, ref Matrix matrix, Vector3[] destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            if (destination.Length < source.Length) throw new ArgumentOutOfRangeException("destination");

            for (int i = 0; i < source.Length; ++i)
            {
                TransformNormal(ref source[i], ref matrix, out destination[i]);
            }
        }

        /// <summary>
        /// ベクトルを同次変換します。
        /// </summary>
        /// <param name="vector">ベクトル。</param>
        /// <param name="matrix">変換行列。</param>
        /// <returns>同次変換されたベクトル。</returns>
        public static Vector3 TransformCoordinate(Vector3 vector, Matrix matrix)
        {
            Vector3 result;
            TransformCoordinate(ref vector, ref matrix, out result);
            return result;
        }

        /// <summary>
        /// ベクトルを同次変換します。
        /// </summary>
        /// <param name="vector">ベクトル。</param>
        /// <param name="matrix">変換行列。</param>
        /// <param name="result">同次変換されたベクトル。</param>
        public static void TransformCoordinate(ref Vector3 vector, ref Matrix matrix, out Vector3 result)
        {
            Vector4 transformed;
            Vector4.Transform(ref vector, ref matrix, out transformed);

            Vector4 homogeneous;
            Vector4.Divide(ref transformed, transformed.W, out homogeneous);

            result = new Vector3(homogeneous.X, homogeneous.Y, homogeneous.Z);
        }

        /// <summary>
        /// ベクトルを同次変換します。
        /// </summary>
        /// <param name="source">ベクトルの配列。</param>
        /// <param name="matrix">変換行列。</param>
        /// <param name="destination">同次変換されたベクトルの配列。</param>
        public static void TransformCoordinate(Vector3[] source, ref Matrix matrix, Vector3[] destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            if (destination.Length < source.Length) throw new ArgumentOutOfRangeException("destination");

            for (int i = 0; i < source.Length; ++i)
            {
                TransformCoordinate(ref source[i], ref matrix, out destination[i]);
            }
        }

        #region operator

        public static Vector3 operator -(Vector3 value)
        {
            Vector3 result;
            Negate(ref value, out result);
            return result;
        }

        public static Vector3 operator +(Vector3 left, Vector3 right)
        {
            Vector3 result;
            Add(ref left, ref right, out result);
            return result;
        }

        public static Vector3 operator -(Vector3 left, Vector3 right)
        {
            Vector3 result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        public static Vector3 operator *(Vector3 value, float scale)
        {
            Vector3 result;
            Multiply(ref value, scale, out result);
            return result;
        }

        public static Vector3 operator *(float scale, Vector3 value)
        {
            Vector3 result;
            Multiply(ref value, scale, out result);
            return result;
        }

        public static Vector3 operator *(Vector3 left, Vector3 right)
        {
            Vector3 result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        public static Vector3 operator /(Vector3 value, float scale)
        {
            Vector3 result;
            Divide(ref value, scale, out result);
            return result;
        }

        public static Vector3 operator /(Vector3 left, Vector3 right)
        {
            Vector3 result;
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
            return X * X + Y * Y + Z * Z;
        }

        public void Normalize()
        {
            Normalize(ref this, out this);
        }

        public Vector4 ToVector4()
        {
            return new Vector4(X, Y, Z, 0);
        }

        /// <summary>
        /// ゼロ ベクトルであるか否かを検査します。
        /// </summary>
        /// <returns>
        /// true (ゼロ ベクトルである場合)、false (それ以外の場合)。
        /// </returns>
        public bool IsZero()
        {
            return X == 0 && Y == 0 && Z == 0;
        }

        #region IEquatable

        public static bool operator ==(Vector3 left, Vector3 right)
        {
            return left.Equals(ref right);
        }

        public static bool operator !=(Vector3 left, Vector3 right)
        {
            return !left.Equals(ref right);
        }

        public bool Equals(Vector3 other, float tolerance)
        {
            return Equals(ref other, tolerance);
        }

        public bool Equals(ref Vector3 other, float tolerance)
        {
            return ((float) Math.Abs(other.X - X) < tolerance &&
                (float) Math.Abs(other.Y - Y) < tolerance &&
                (float) Math.Abs(other.Z - Z) < tolerance);
        }

        public bool Equals(Vector3 other)
        {
            return Equals(ref other);
        }

        public bool Equals(ref Vector3 other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((Vector3) obj);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{X:" + X + " Y:" + Y + " Z:" + Z + "}";
        }

        #endregion
    }
}
