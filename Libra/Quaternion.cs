#region Using

using System;
using System.Runtime.InteropServices;

#endregion

// SharpDX.Quaternion から移植。
// 一部インタフェースを XNA 形式へ変更。
// 一部ロジックを変更。

namespace Libra
{
    public struct Quaternion : IEquatable<Quaternion>
    {
        public static readonly Quaternion Zero = new Quaternion();

        public static readonly Quaternion One = new Quaternion(1, 1, 1, 1);

        public static readonly Quaternion Identity = new Quaternion(0, 0, 0, 1);

        public float X;

        public float Y;

        public float Z;

        public float W;

        public Quaternion(Vector4 value)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
            W = value.W;
        }

        public Quaternion(Vector3 value, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
            W = w;
        }

        public Quaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static Quaternion Add(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Add(ref left, ref right, out result);
            return result;
        }

        public static void Add(ref Quaternion left, ref Quaternion right, out Quaternion result)
        {
            result.X = left.X + right.X;
            result.Y = left.Y + right.Y;
            result.Z = left.Z + right.Z;
            result.W = left.W + right.W;
        }

        public static Quaternion Concatenate(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Concatenate(ref left, ref right, out result);
            return result;
        }

        public static void Concatenate(ref Quaternion left, ref Quaternion right, out Quaternion result)
        {
            result.X = (left.W * right.X) + (left.X * right.W) + (left.Z * right.Y) - (left.Y * right.Z);
            result.Y = (left.W * right.Y) + (left.Y * right.W) + (left.X * right.Z) - (left.Z * right.X);
            result.Z = (left.W * right.Z) + (left.Z * right.W) + (left.Y * right.X) - (left.X * right.Y);
            result.W = (left.W * right.W) - (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
        }

        public void Conjugate()
        {
            X = -X;
            Y = -Y;
            Z = -Z;
        }

        public static Quaternion Conjugate(Quaternion value)
        {
            Quaternion result;
            Conjugate(ref value, out result);
            return result;
        }

        public static void Conjugate(ref Quaternion value, out Quaternion result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.W = value.W;
        }

        public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle)
        {
            Quaternion result;
            CreateFromAxisAngle(ref axis, angle, out result);
            return result;
        }

        public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Quaternion result)
        {
            Vector3 normalized;
            Vector3.Normalize(ref axis, out normalized);

            float half = angle * 0.5f;
            float sin = (float) Math.Sin(half);
            float cos = (float) Math.Cos(half);

            result.X = normalized.X * sin;
            result.Y = normalized.Y * sin;
            result.Z = normalized.Z * sin;
            result.W = cos;
        }

        public static Quaternion CreateFromRotationMatrix(Matrix matrix)
        {
            Quaternion result;
            CreateFromRotationMatrix(ref matrix, out result);
            return result;
        }

        public static void CreateFromRotationMatrix(ref Matrix matrix, out Quaternion result)
        {
            float sqrt;
            float half;
            float scale = matrix.M11 + matrix.M22 + matrix.M33;

            if (scale > 0.0f)
            {
                sqrt = (float) Math.Sqrt(scale + 1.0f);
                result.W = sqrt * 0.5f;
                sqrt = 0.5f / sqrt;

                result.X = (matrix.M23 - matrix.M32) * sqrt;
                result.Y = (matrix.M31 - matrix.M13) * sqrt;
                result.Z = (matrix.M12 - matrix.M21) * sqrt;
            }
            else if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
            {
                sqrt = (float) Math.Sqrt(1.0f + matrix.M11 - matrix.M22 - matrix.M33);
                half = 0.5f / sqrt;

                result.X = 0.5f * sqrt;
                result.Y = (matrix.M12 + matrix.M21) * half;
                result.Z = (matrix.M13 + matrix.M31) * half;
                result.W = (matrix.M23 - matrix.M32) * half;
            }
            else if (matrix.M22 > matrix.M33)
            {
                sqrt = (float) Math.Sqrt(1.0f + matrix.M22 - matrix.M11 - matrix.M33);
                half = 0.5f / sqrt;

                result.X = (matrix.M21 + matrix.M12) * half;
                result.Y = 0.5f * sqrt;
                result.Z = (matrix.M32 + matrix.M23) * half;
                result.W = (matrix.M31 - matrix.M13) * half;
            }
            else
            {
                sqrt = (float) Math.Sqrt(1.0f + matrix.M33 - matrix.M11 - matrix.M22);
                half = 0.5f / sqrt;

                result.X = (matrix.M31 + matrix.M13) * half;
                result.Y = (matrix.M32 + matrix.M23) * half;
                result.Z = 0.5f * sqrt;
                result.W = (matrix.M12 - matrix.M21) * half;
            }
        }

        public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll)
        {
            Quaternion result;
            CreateFromYawPitchRoll(yaw, pitch, roll, out result);
            return result;
        }

        public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Quaternion result)
        {
            // Euler angles: phi * theta * psi
            // phi:   z-axis
            // theta: x'-axis
            // psi:   z'-axis

            float halfRoll = roll * 0.5f;
            float halfPitch = pitch * 0.5f;
            float halfYaw = yaw * 0.5f;

            float sinRoll = (float) Math.Sin(halfRoll);
            float cosRoll = (float) Math.Cos(halfRoll);
            float sinPitch = (float) Math.Sin(halfPitch);
            float cosPitch = (float) Math.Cos(halfPitch);
            float sinYaw = (float) Math.Sin(halfYaw);
            float cosYaw = (float) Math.Cos(halfYaw);

            result.X = (cosYaw * sinPitch * cosRoll) + (sinYaw * cosPitch * sinRoll);
            result.Y = (sinYaw * cosPitch * cosRoll) - (cosYaw * sinPitch * sinRoll);
            result.Z = (cosYaw * cosPitch * sinRoll) - (sinYaw * sinPitch * cosRoll);
            result.W = (cosYaw * cosPitch * cosRoll) + (sinYaw * sinPitch * sinRoll);
        }

        public void Invert()
        {
            float lengthSq = LengthSquared();
            if (lengthSq > MathHelper.ZeroTolerance)
            {
                lengthSq = 1.0f / lengthSq;

                X = -X * lengthSq;
                Y = -Y * lengthSq;
                Z = -Z * lengthSq;
                W = W * lengthSq;
            }
        }

        public static Quaternion Divide(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Divide(ref left, ref right, out result);
            return result;
        }

        public static void Divide(ref Quaternion left, ref Quaternion right, out Quaternion result)
        {
            float factor = 1f / ((right.X * right.X) + (right.Y * right.Y) + (right.Z * right.Z) + (right.W * right.W));
            result.X = ((left.X * right.W) - (right.X * left.W) - (left.Y * right.Z) + (left.Z * right.Y)) * factor;
            result.Y = ((left.Y * right.W) - (right.Y * left.W) - (left.Z * right.X) + (left.X * right.Z)) * factor;
            result.Z = ((left.Z * right.W) - (right.Z * left.W) - (left.X * right.Y) + (left.Y * right.X)) * factor;
            result.W = ((left.W * right.W) + (right.X * left.X) - (left.Y * right.Y) - (left.Z * right.Z)) * factor;
        }

        public static float Dot(Quaternion left, Quaternion right)
        {
            return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z) + (left.W * right.W);
        }

        public static void Dot(ref Quaternion left, ref Quaternion right, out float result)
        {
            result = (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z) + (left.W * right.W);
        }

        public float Length()
        {
            return (float) Math.Sqrt((X * X) + (Y * Y) + (Z * Z) + (W * W));
        }

        public float LengthSquared()
        {
            return (X * X) + (Y * Y) + (Z * Z) + (W * W);
        }

        public void Normalize()
        {
            float length = Length();
            if (length > MathHelper.ZeroTolerance)
            {
                float inverse = 1.0f / length;
                X *= inverse;
                Y *= inverse;
                Z *= inverse;
                W *= inverse;
            }
        }

        public static void Subtract(ref Quaternion left, ref Quaternion right, out Quaternion result)
        {
            result.X = left.X - right.X;
            result.Y = left.Y - right.Y;
            result.Z = left.Z - right.Z;
            result.W = left.W - right.W;
        }

        public static Quaternion Subtract(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        public static void Multiply(ref Quaternion value, float scale, out Quaternion result)
        {
            result.X = value.X * scale;
            result.Y = value.Y * scale;
            result.Z = value.Z * scale;
            result.W = value.W * scale;
        }

        public static Quaternion Multiply(Quaternion value, float scale)
        {
            Quaternion result;
            Multiply(ref value, scale, out result);
            return result;
        }

        public static void Multiply(ref Quaternion left, ref Quaternion right, out Quaternion result)
        {
            // SharpDX は式に誤りがある。

            float lx = left.X;
            float ly = left.Y;
            float lz = left.Z;
            float lw = left.W;
            float rx = right.X;
            float ry = right.Y;
            float rz = right.Z;
            float rw = right.W;

            // 誤: SharpDX。
            //result.W = rw * lw - rx * lx - ry * ly - rz * lz;
            //result.X = rx * lw + lx * rw + ry * lz - rz * ly;
            //result.Y = ry * lw + ly * rw + rz * lx - rx * lz;
            //result.Z = rz * lw + lz * rw + rx * ly - ry * lx;

            // 正
            result.W = lw * rw - lx * rx - ly * ry - lz * rz;
            result.X = lw * rx + lx * rw + ly * rz - lz * ry;
            result.Y = lw * ry + ly * rw + lz * rx - lx * rz;
            result.Z = lw * rz + lz * rw + lx * ry - ly * rx;
        }

        public static Quaternion Multiply(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        public static void Negate(ref Quaternion value, out Quaternion result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.W = -value.W;
        }

        public static Quaternion Negate(Quaternion value)
        {
            Quaternion result;
            Negate(ref value, out result);
            return result;
        }

        public static void Invert(ref Quaternion value, out Quaternion result)
        {
            result = value;
            result.Invert();
        }

        public static Quaternion Invert(Quaternion value)
        {
            Quaternion result;
            Invert(ref value, out result);
            return result;
        }

        public static void Lerp(ref Quaternion start, ref Quaternion end, float amount, out Quaternion result)
        {
            float inverse = 1.0f - amount;

            if (Dot(start, end) >= 0.0f)
            {
                result.X = (inverse * start.X) + (amount * end.X);
                result.Y = (inverse * start.Y) + (amount * end.Y);
                result.Z = (inverse * start.Z) + (amount * end.Z);
                result.W = (inverse * start.W) + (amount * end.W);
            }
            else
            {
                result.X = (inverse * start.X) - (amount * end.X);
                result.Y = (inverse * start.Y) - (amount * end.Y);
                result.Z = (inverse * start.Z) - (amount * end.Z);
                result.W = (inverse * start.W) - (amount * end.W);
            }

            result.Normalize();
        }

        public static Quaternion Lerp(Quaternion start, Quaternion end, float amount)
        {
            Quaternion result;
            Lerp(ref start, ref end, amount, out result);
            return result;
        }

        public static void Normalize(ref Quaternion value, out Quaternion result)
        {
            Quaternion temp = value;
            result = temp;
            result.Normalize();
        }

        public static Quaternion Normalize(Quaternion value)
        {
            value.Normalize();
            return value;
        }

        public static void Slerp(ref Quaternion start, ref Quaternion end, float amount, out Quaternion result)
        {
            float opposite;
            float inverse;
            float dot = Dot(start, end);

            if (Math.Abs(dot) > 1.0f - MathHelper.ZeroTolerance)
            {
                inverse = 1.0f - amount;
                opposite = amount * Math.Sign(dot);
            }
            else
            {
                float acos = (float) Math.Acos(Math.Abs(dot));
                float invSin = (float) (1.0 / Math.Sin(acos));

                inverse = (float) Math.Sin((1.0f - amount) * acos) * invSin;
                opposite = (float) Math.Sin(amount * acos) * invSin * Math.Sign(dot);
            }

            result.X = (inverse * start.X) + (opposite * end.X);
            result.Y = (inverse * start.Y) + (opposite * end.Y);
            result.Z = (inverse * start.Z) + (opposite * end.Z);
            result.W = (inverse * start.W) + (opposite * end.W);
        }

        public static Quaternion Slerp(Quaternion start, Quaternion end, float amount)
        {
            Quaternion result;
            Slerp(ref start, ref end, amount, out result);
            return result;
        }

        public static Quaternion operator +(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Add(ref left, ref right, out result);
            return result;
        }

        public static Quaternion operator -(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        public static Quaternion operator -(Quaternion value)
        {
            Quaternion result;
            Negate(ref value, out result);
            return result;
        }

        public static Quaternion operator *(float scale, Quaternion value)
        {
            Quaternion result;
            Multiply(ref value, scale, out result);
            return result;
        }

        public static Quaternion operator *(Quaternion value, float scale)
        {
            Quaternion result;
            Multiply(ref value, scale, out result);
            return result;
        }

        public static Quaternion operator *(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        #region IEquatable

        public static bool operator ==(Quaternion left, Quaternion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Quaternion left, Quaternion right)
        {
            return !left.Equals(right);
        }

        public bool Equals(Quaternion other)
        {
            return ((float) Math.Abs(other.X - X) < MathHelper.ZeroTolerance &&
                (float) Math.Abs(other.Y - Y) < MathHelper.ZeroTolerance &&
                (float) Math.Abs(other.Z - Z) < MathHelper.ZeroTolerance &&
                (float) Math.Abs(other.W - W) < MathHelper.ZeroTolerance);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((Quaternion) obj);
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
