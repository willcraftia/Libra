﻿#region Using

using System;

#endregion

// SharpDX.Quaternion から移植。
// 一部インタフェースを XNA 形式へ変更。
// 一部ロジックを変更。
// 最新バージョンでは修正済みかもしれないが、SharpDX.Quaternion の
// Multiply は式に誤りがあるため注意 (Issue として挙がっている)。
// また、この影響か、MonoGame の Divide も式に誤りがあるため注意。

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
            Multiply(ref right, ref left, out result);
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

        public static Quaternion Divide(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Divide(ref left, ref right, out result);
            return result;
        }

        public static void Divide(ref Quaternion left, ref Quaternion right, out Quaternion result)
        {
            // 四元数の除算は [left] * [right の逆四元数]。

            Quaternion invertRight;
            Invert(ref right, out invertRight);

            Multiply(ref left, ref invertRight, out result);
        }

        public static float Dot(Quaternion left, Quaternion right)
        {
            return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z) + (left.W * right.W);
        }

        public static void Dot(ref Quaternion left, ref Quaternion right, out float result)
        {
            result = (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z) + (left.W * right.W);
        }

        // XNA では Inverse メソッドだが、
        // Matrix の Invert メソッドに合わせて Quaternion も Invert とする。

        public void Invert()
        {
            float lengthSquared = (X * X) + (Y * Y) + (Z * Z) + (W * W);
            if (0.0f < lengthSquared)
            {
                var factor = 1.0f / lengthSquared;
                W *= factor;
                X *= factor;
                Y *= factor;
                Z *= factor;
                X = -X;
                Y = -Y;
                Z = -Z;
            }
        }

        public static Quaternion Invert(Quaternion value)
        {
            Quaternion result;
            Invert(ref value, out result);
            return result;
        }

        public static void Invert(ref Quaternion value, out Quaternion result)
        {
            result = value;
            result.Invert();
        }

        public float Length()
        {
            return (float) Math.Sqrt((X * X) + (Y * Y) + (Z * Z) + (W * W));
        }

        public float LengthSquared()
        {
            return (X * X) + (Y * Y) + (Z * Z) + (W * W);
        }

        public static Quaternion Lerp(Quaternion start, Quaternion end, float amount)
        {
            Quaternion result;
            Lerp(ref start, ref end, amount, out result);
            return result;
        }

        public static void Lerp(ref Quaternion start, ref Quaternion end, float amount, out Quaternion result)
        {
            float inverse = 1.0f - amount;

            float dot;
            Dot(ref start, ref end, out dot);

            if (0.0f <= dot)
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

        public static Quaternion Multiply(Quaternion value, float scale)
        {
            Quaternion result;
            Multiply(ref value, scale, out result);
            return result;
        }

        public static void Multiply(ref Quaternion value, float scale, out Quaternion result)
        {
            result.X = value.X * scale;
            result.Y = value.Y * scale;
            result.Z = value.Z * scale;
            result.W = value.W * scale;
        }

        public static Quaternion Multiply(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        public static void Multiply(ref Quaternion left, ref Quaternion right, out Quaternion result)
        {
            float lw = left.W;
            float lx = left.X;
            float ly = left.Y;
            float lz = left.Z;

            float rw = right.W;
            float rx = right.X;
            float ry = right.Y;
            float rz = right.Z;

            result.W = lw * rw - lx * rx - ly * ry - lz * rz;
            result.X = lw * rx + lx * rw + ly * rz - lz * ry;
            result.Y = lw * ry + ly * rw + lz * rx - lx * rz;
            result.Z = lw * rz + lz * rw + lx * ry - ly * rx;
        }

        public static Quaternion Negate(Quaternion value)
        {
            Quaternion result;
            Negate(ref value, out result);
            return result;
        }

        public static void Negate(ref Quaternion value, out Quaternion result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.W = -value.W;
        }

        public void Normalize()
        {
            float length = Length();
            if (0.0f < length)
            {
                float factor = 1.0f / length;
                X *= factor;
                Y *= factor;
                Z *= factor;
                W *= factor;
            }
        }

        public static Quaternion Normalize(Quaternion value)
        {
            value.Normalize();
            return value;
        }

        public static void Normalize(ref Quaternion value, out Quaternion result)
        {
            result = value;
            result.Normalize();
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

        public static Quaternion Subtract(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        public static void Subtract(ref Quaternion left, ref Quaternion right, out Quaternion result)
        {
            result.X = left.X - right.X;
            result.Y = left.Y - right.Y;
            result.Z = left.Z - right.Z;
            result.W = left.W - right.W;
        }

        public static void CreateRotationBetween(ref Vector3 start, ref Vector3 destination, out Quaternion result)
        {
            // http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-17-quaternions/

            var v0 = start;
            var v1 = destination;

            v0.Normalize();
            v1.Normalize();

            float dot;
            Vector3.Dot(ref v0, ref v1, out dot);

            if (1.0f <= dot)
            {
                // 同じベクトルならば回転無し。
                result = Quaternion.Identity;
            }
            else if (dot < (MathHelper.ZeroTolerance - 1.0f))
            {
                // ベクトル同士の方向が真逆になる場合は特殊。
                // この場合、理想的な回転軸が存在しないため、回転軸の推測を行う。
                // 回転軸は、ベクトル start に対して垂直であれば良い。
                var temp = Vector3.UnitZ;
                Vector3 axis;
                Vector3.Cross(ref temp, ref start, out axis);

                if (axis.IsZero())
                {
                    temp = Vector3.UnitX;
                    Vector3.Cross(ref temp, ref start, out axis);
                }

                axis.Normalize();

                CreateFromAxisAngle(ref axis, MathHelper.Pi, out result);
            }
            else
            {
                float s = (float) Math.Sqrt((1.0f + dot) * 2);
                float invs = 1 / s;

                Vector3 axis;
                Vector3.Cross(ref start, ref destination, out axis);

                result.X = axis.X * invs;
                result.Y = axis.Y * invs;
                result.Z = axis.Z * invs;
                result.W = s * 0.5f;
            }
        }

        #region operator

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

        #endregion

        #region IEquatable

        public static bool operator ==(Quaternion left, Quaternion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Quaternion left, Quaternion right)
        {
            return !left.Equals(right);
        }

        public bool Equals(Quaternion other, float tolerance)
        {
            return Equals(ref other, tolerance);
        }

        public bool Equals(ref Quaternion other, float tolerance)
        {
            return ((float) Math.Abs(other.X - X) < tolerance &&
                (float) Math.Abs(other.Y - Y) < tolerance &&
                (float) Math.Abs(other.Z - Z) < tolerance &&
                (float) Math.Abs(other.W - W) < tolerance);
        }

        public bool Equals(Quaternion other)
        {
            return X == other.X && Y == other.Y && Z == other.Z && W == other.W;
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
