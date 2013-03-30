#region Using

using System;
using System.Runtime.InteropServices;

#endregion

// SharpDX.Matrix から移植。
// 一部インタフェースを XNA 形式へ変更。
// 一部ロジックを変更。

namespace Libra
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix : IEquatable<Matrix>
    {
        public static readonly Matrix Zero = new Matrix();

        public static readonly Matrix Identity = new Matrix { M11 = 1.0f, M22 = 1.0f, M33 = 1.0f, M44 = 1.0f };

        public float M11;

        public float M12;

        public float M13;

        public float M14;

        public float M21;

        public float M22;

        public float M23;

        public float M24;

        public float M31;

        public float M32;

        public float M33;

        public float M34;

        public float M41;

        public float M42;

        public float M43;

        public float M44;

        public Vector3 Up
        {
            get { return new Vector3(M21, M22, M23); }
            set
            {
                M21 = value.X;
                M22 = value.Y;
                M23 = value.Z;
            }
        }

        public Vector3 Down
        {
            get { return new Vector3(-M21, -M22, -M23); }
            set
            {
                M21 = -value.X;
                M22 = -value.Y;
                M23 = -value.Z;
            }
        }

        public Vector3 Right
        {
            get { return new Vector3(M11, M12, M13); }
            set
            {
                M11 = value.X;
                M12 = value.Y;
                M13 = value.Z;
            }
        }

        public Vector3 Left
        {
            get { return new Vector3(-M11, -M12, -M13); }
            set
            {
                M11 = -value.X;
                M12 = -value.Y;
                M13 = -value.Z;
            }
        }

        public Vector3 Forward
        {
            get { return new Vector3(-M31, -M32, -M33); }
            set
            {
                M31 = -value.X;
                M32 = -value.Y;
                M33 = -value.Z;
            }
        }

        public Vector3 Backward
        {
            get { return new Vector3(M31, M32, M33); }
            set
            {
                M31 = value.X;
                M32 = value.Y;
                M33 = value.Z;
            }
        }

        public Vector3 Translation
        {
            get { return new Vector3(M41, M42, M43); }
            set
            {
                M41 = value.X;
                M42 = value.Y;
                M43 = value.Z;
            }
        }

        public Matrix(float value)
        {
            M11 = M12 = M13 = M14 =
            M21 = M22 = M23 = M24 =
            M31 = M32 = M33 = M34 =
            M41 = M42 = M43 = M44 = value;
        }

        public Matrix(float m11, float m12, float m13, float m14,
                      float m21, float m22, float m23, float m24,
                      float m31, float m32, float m33, float m34,
                      float m41, float m42, float m43, float m44)
        {
            M11 = m11; M12 = m12; M13 = m13; M14 = m14;
            M21 = m21; M22 = m22; M23 = m23; M24 = m24;
            M31 = m31; M32 = m32; M33 = m33; M34 = m34;
            M41 = m41; M42 = m42; M43 = m43; M44 = m44;
        }

        public float Determinant()
        {
            float temp1 = (M33 * M44) - (M34 * M43);
            float temp2 = (M32 * M44) - (M34 * M42);
            float temp3 = (M32 * M43) - (M33 * M42);
            float temp4 = (M31 * M44) - (M34 * M41);
            float temp5 = (M31 * M43) - (M33 * M41);
            float temp6 = (M31 * M42) - (M32 * M41);

            return ((((M11 * (((M22 * temp1) - (M23 * temp2)) + (M24 * temp3))) - (M12 * (((M21 * temp1) -
                (M23 * temp4)) + (M24 * temp5)))) + (M13 * (((M21 * temp2) - (M22 * temp4)) + (M24 * temp6)))) -
                (M14 * (((M21 * temp3) - (M22 * temp5)) + (M23 * temp6))));
        }

        public bool Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation)
        {
            //Source: Unknown
            //References: http://www.gamedev.net/community/forums/topic.asp?topic_id=441695

            translation = new Vector3(M41, M42, M43);

            //Scaling is the length of the rows.
            scale.X = (float) Math.Sqrt((M11 * M11) + (M12 * M12) + (M13 * M13));
            scale.Y = (float) Math.Sqrt((M21 * M21) + (M22 * M22) + (M23 * M23));
            scale.Z = (float) Math.Sqrt((M31 * M31) + (M32 * M32) + (M33 * M33));

            //If any of the scaling factors are zero, than the rotation matrix can not exist.
            if (Math.Abs(scale.X) < MathHelper.ZeroTolerance ||
                Math.Abs(scale.Y) < MathHelper.ZeroTolerance ||
                Math.Abs(scale.Z) < MathHelper.ZeroTolerance)
            {
                rotation = Quaternion.Identity;
                return false;
            }

            //The rotation is the left over matrix after dividing out the scaling.
            Matrix rotationmatrix = new Matrix();
            rotationmatrix.M11 = M11 / scale.X;
            rotationmatrix.M12 = M12 / scale.X;
            rotationmatrix.M13 = M13 / scale.X;

            rotationmatrix.M21 = M21 / scale.Y;
            rotationmatrix.M22 = M22 / scale.Y;
            rotationmatrix.M23 = M23 / scale.Y;

            rotationmatrix.M31 = M31 / scale.Z;
            rotationmatrix.M32 = M32 / scale.Z;
            rotationmatrix.M33 = M33 / scale.Z;

            rotationmatrix.M44 = 1f;

            Quaternion.RotationMatrix(ref rotationmatrix, out rotation);
            return true;
        }

        public static void Add(ref Matrix left, ref Matrix right, out Matrix result)
        {
            result.M11 = left.M11 + right.M11;
            result.M12 = left.M12 + right.M12;
            result.M13 = left.M13 + right.M13;
            result.M14 = left.M14 + right.M14;
            result.M21 = left.M21 + right.M21;
            result.M22 = left.M22 + right.M22;
            result.M23 = left.M23 + right.M23;
            result.M24 = left.M24 + right.M24;
            result.M31 = left.M31 + right.M31;
            result.M32 = left.M32 + right.M32;
            result.M33 = left.M33 + right.M33;
            result.M34 = left.M34 + right.M34;
            result.M41 = left.M41 + right.M41;
            result.M42 = left.M42 + right.M42;
            result.M43 = left.M43 + right.M43;
            result.M44 = left.M44 + right.M44;
        }

        public static Matrix Add(Matrix left, Matrix right)
        {
            Matrix result;
            Add(ref left, ref right, out result);
            return result;
        }

        public static void Subtract(ref Matrix left, ref Matrix right, out Matrix result)
        {
            result.M11 = left.M11 - right.M11;
            result.M12 = left.M12 - right.M12;
            result.M13 = left.M13 - right.M13;
            result.M14 = left.M14 - right.M14;
            result.M21 = left.M21 - right.M21;
            result.M22 = left.M22 - right.M22;
            result.M23 = left.M23 - right.M23;
            result.M24 = left.M24 - right.M24;
            result.M31 = left.M31 - right.M31;
            result.M32 = left.M32 - right.M32;
            result.M33 = left.M33 - right.M33;
            result.M34 = left.M34 - right.M34;
            result.M41 = left.M41 - right.M41;
            result.M42 = left.M42 - right.M42;
            result.M43 = left.M43 - right.M43;
            result.M44 = left.M44 - right.M44;
        }

        public static Matrix Subtract(Matrix left, Matrix right)
        {
            Matrix result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        public static void Multiply(ref Matrix left, float right, out Matrix result)
        {
            result.M11 = left.M11 * right;
            result.M12 = left.M12 * right;
            result.M13 = left.M13 * right;
            result.M14 = left.M14 * right;
            result.M21 = left.M21 * right;
            result.M22 = left.M22 * right;
            result.M23 = left.M23 * right;
            result.M24 = left.M24 * right;
            result.M31 = left.M31 * right;
            result.M32 = left.M32 * right;
            result.M33 = left.M33 * right;
            result.M34 = left.M34 * right;
            result.M41 = left.M41 * right;
            result.M42 = left.M42 * right;
            result.M43 = left.M43 * right;
            result.M44 = left.M44 * right;
        }

        public static Matrix Multiply(Matrix left, float right)
        {
            Matrix result;
            Multiply(ref left, right, out result);
            return result;
        }

        public static void Multiply(ref Matrix left, ref Matrix right, out Matrix result)
        {
            result.M11 = (left.M11 * right.M11) + (left.M12 * right.M21) + (left.M13 * right.M31) + (left.M14 * right.M41);
            result.M12 = (left.M11 * right.M12) + (left.M12 * right.M22) + (left.M13 * right.M32) + (left.M14 * right.M42);
            result.M13 = (left.M11 * right.M13) + (left.M12 * right.M23) + (left.M13 * right.M33) + (left.M14 * right.M43);
            result.M14 = (left.M11 * right.M14) + (left.M12 * right.M24) + (left.M13 * right.M34) + (left.M14 * right.M44);
            result.M21 = (left.M21 * right.M11) + (left.M22 * right.M21) + (left.M23 * right.M31) + (left.M24 * right.M41);
            result.M22 = (left.M21 * right.M12) + (left.M22 * right.M22) + (left.M23 * right.M32) + (left.M24 * right.M42);
            result.M23 = (left.M21 * right.M13) + (left.M22 * right.M23) + (left.M23 * right.M33) + (left.M24 * right.M43);
            result.M24 = (left.M21 * right.M14) + (left.M22 * right.M24) + (left.M23 * right.M34) + (left.M24 * right.M44);
            result.M31 = (left.M31 * right.M11) + (left.M32 * right.M21) + (left.M33 * right.M31) + (left.M34 * right.M41);
            result.M32 = (left.M31 * right.M12) + (left.M32 * right.M22) + (left.M33 * right.M32) + (left.M34 * right.M42);
            result.M33 = (left.M31 * right.M13) + (left.M32 * right.M23) + (left.M33 * right.M33) + (left.M34 * right.M43);
            result.M34 = (left.M31 * right.M14) + (left.M32 * right.M24) + (left.M33 * right.M34) + (left.M34 * right.M44);
            result.M41 = (left.M41 * right.M11) + (left.M42 * right.M21) + (left.M43 * right.M31) + (left.M44 * right.M41);
            result.M42 = (left.M41 * right.M12) + (left.M42 * right.M22) + (left.M43 * right.M32) + (left.M44 * right.M42);
            result.M43 = (left.M41 * right.M13) + (left.M42 * right.M23) + (left.M43 * right.M33) + (left.M44 * right.M43);
            result.M44 = (left.M41 * right.M14) + (left.M42 * right.M24) + (left.M43 * right.M34) + (left.M44 * right.M44);
        }

        public static Matrix Multiply(Matrix left, Matrix right)
        {
            Matrix result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        public static void Divide(ref Matrix left, float right, out Matrix result)
        {
            float inv = 1.0f / right;

            result.M11 = left.M11 * inv;
            result.M12 = left.M12 * inv;
            result.M13 = left.M13 * inv;
            result.M14 = left.M14 * inv;
            result.M21 = left.M21 * inv;
            result.M22 = left.M22 * inv;
            result.M23 = left.M23 * inv;
            result.M24 = left.M24 * inv;
            result.M31 = left.M31 * inv;
            result.M32 = left.M32 * inv;
            result.M33 = left.M33 * inv;
            result.M34 = left.M34 * inv;
            result.M41 = left.M41 * inv;
            result.M42 = left.M42 * inv;
            result.M43 = left.M43 * inv;
            result.M44 = left.M44 * inv;
        }

        public static Matrix Divide(Matrix left, float right)
        {
            Matrix result;
            Divide(ref left, right, out result);
            return result;
        }

        public static void Divide(ref Matrix left, ref Matrix right, out Matrix result)
        {
            result.M11 = left.M11 / right.M11;
            result.M12 = left.M12 / right.M12;
            result.M13 = left.M13 / right.M13;
            result.M14 = left.M14 / right.M14;
            result.M21 = left.M21 / right.M21;
            result.M22 = left.M22 / right.M22;
            result.M23 = left.M23 / right.M23;
            result.M24 = left.M24 / right.M24;
            result.M31 = left.M31 / right.M31;
            result.M32 = left.M32 / right.M32;
            result.M33 = left.M33 / right.M33;
            result.M34 = left.M34 / right.M34;
            result.M41 = left.M41 / right.M41;
            result.M42 = left.M42 / right.M42;
            result.M43 = left.M43 / right.M43;
            result.M44 = left.M44 / right.M44;
        }

        public static Matrix Divide(Matrix left, Matrix right)
        {
            Matrix result;
            Divide(ref left, ref right, out result);
            return result;
        }

        public static void Negate(ref Matrix value, out Matrix result)
        {
            result.M11 = -value.M11;
            result.M12 = -value.M12;
            result.M13 = -value.M13;
            result.M14 = -value.M14;
            result.M21 = -value.M21;
            result.M22 = -value.M22;
            result.M23 = -value.M23;
            result.M24 = -value.M24;
            result.M31 = -value.M31;
            result.M32 = -value.M32;
            result.M33 = -value.M33;
            result.M34 = -value.M34;
            result.M41 = -value.M41;
            result.M42 = -value.M42;
            result.M43 = -value.M43;
            result.M44 = -value.M44;
        }

        public static Matrix Negate(Matrix value)
        {
            Matrix result;
            Negate(ref value, out result);
            return result;
        }

        public static void Lerp(ref Matrix start, ref Matrix end, float amount, out Matrix result)
        {
            result.M11 = start.M11 + ((end.M11 - start.M11) * amount);
            result.M12 = start.M12 + ((end.M12 - start.M12) * amount);
            result.M13 = start.M13 + ((end.M13 - start.M13) * amount);
            result.M14 = start.M14 + ((end.M14 - start.M14) * amount);
            result.M21 = start.M21 + ((end.M21 - start.M21) * amount);
            result.M22 = start.M22 + ((end.M22 - start.M22) * amount);
            result.M23 = start.M23 + ((end.M23 - start.M23) * amount);
            result.M24 = start.M24 + ((end.M24 - start.M24) * amount);
            result.M31 = start.M31 + ((end.M31 - start.M31) * amount);
            result.M32 = start.M32 + ((end.M32 - start.M32) * amount);
            result.M33 = start.M33 + ((end.M33 - start.M33) * amount);
            result.M34 = start.M34 + ((end.M34 - start.M34) * amount);
            result.M41 = start.M41 + ((end.M41 - start.M41) * amount);
            result.M42 = start.M42 + ((end.M42 - start.M42) * amount);
            result.M43 = start.M43 + ((end.M43 - start.M43) * amount);
            result.M44 = start.M44 + ((end.M44 - start.M44) * amount);
        }

        public static Matrix Lerp(Matrix start, Matrix end, float amount)
        {
            Matrix result;
            Lerp(ref start, ref end, amount, out result);
            return result;
        }

        public static void SmoothStep(ref Matrix start, ref Matrix end, float amount, out Matrix result)
        {
            amount = (amount > 1.0f) ? 1.0f : ((amount < 0.0f) ? 0.0f : amount);
            amount = (amount * amount) * (3.0f - (2.0f * amount));

            result.M11 = start.M11 + ((end.M11 - start.M11) * amount);
            result.M12 = start.M12 + ((end.M12 - start.M12) * amount);
            result.M13 = start.M13 + ((end.M13 - start.M13) * amount);
            result.M14 = start.M14 + ((end.M14 - start.M14) * amount);
            result.M21 = start.M21 + ((end.M21 - start.M21) * amount);
            result.M22 = start.M22 + ((end.M22 - start.M22) * amount);
            result.M23 = start.M23 + ((end.M23 - start.M23) * amount);
            result.M24 = start.M24 + ((end.M24 - start.M24) * amount);
            result.M31 = start.M31 + ((end.M31 - start.M31) * amount);
            result.M32 = start.M32 + ((end.M32 - start.M32) * amount);
            result.M33 = start.M33 + ((end.M33 - start.M33) * amount);
            result.M34 = start.M34 + ((end.M34 - start.M34) * amount);
            result.M41 = start.M41 + ((end.M41 - start.M41) * amount);
            result.M42 = start.M42 + ((end.M42 - start.M42) * amount);
            result.M43 = start.M43 + ((end.M43 - start.M43) * amount);
            result.M44 = start.M44 + ((end.M44 - start.M44) * amount);
        }

        public static Matrix SmoothStep(Matrix start, Matrix end, float amount)
        {
            Matrix result;
            SmoothStep(ref start, ref end, amount, out result);
            return result;
        }

        public static void Transpose(ref Matrix value, out Matrix result)
        {
            var temp = new Matrix(
                value.M11, value.M21, value.M31, value.M41,
                value.M12, value.M22, value.M32, value.M42,
                value.M13, value.M23, value.M33, value.M43,
                value.M14, value.M24, value.M34, value.M44
                );

            result = temp;
        }

        public static Matrix Transpose(Matrix value)
        {
            Matrix result;
            Transpose(ref value, out result);
            return result;
        }

        public static void Invert(ref Matrix value, out Matrix result)
        {
            float b0 = (value.M31 * value.M42) - (value.M32 * value.M41);
            float b1 = (value.M31 * value.M43) - (value.M33 * value.M41);
            float b2 = (value.M34 * value.M41) - (value.M31 * value.M44);
            float b3 = (value.M32 * value.M43) - (value.M33 * value.M42);
            float b4 = (value.M34 * value.M42) - (value.M32 * value.M44);
            float b5 = (value.M33 * value.M44) - (value.M34 * value.M43);

            float d11 = value.M22 * b5 + value.M23 * b4 + value.M24 * b3;
            float d12 = value.M21 * b5 + value.M23 * b2 + value.M24 * b1;
            float d13 = value.M21 * -b4 + value.M22 * b2 + value.M24 * b0;
            float d14 = value.M21 * b3 + value.M22 * -b1 + value.M23 * b0;

            float det = value.M11 * d11 - value.M12 * d12 + value.M13 * d13 - value.M14 * d14;
            if (Math.Abs(det) <= MathHelper.ZeroTolerance)
            {
                result = Matrix.Zero;
                return;
            }

            det = 1f / det;

            float a0 = (value.M11 * value.M22) - (value.M12 * value.M21);
            float a1 = (value.M11 * value.M23) - (value.M13 * value.M21);
            float a2 = (value.M14 * value.M21) - (value.M11 * value.M24);
            float a3 = (value.M12 * value.M23) - (value.M13 * value.M22);
            float a4 = (value.M14 * value.M22) - (value.M12 * value.M24);
            float a5 = (value.M13 * value.M24) - (value.M14 * value.M23);

            float d21 = value.M12 * b5 + value.M13 * b4 + value.M14 * b3;
            float d22 = value.M11 * b5 + value.M13 * b2 + value.M14 * b1;
            float d23 = value.M11 * -b4 + value.M12 * b2 + value.M14 * b0;
            float d24 = value.M11 * b3 + value.M12 * -b1 + value.M13 * b0;

            float d31 = value.M42 * a5 + value.M43 * a4 + value.M44 * a3;
            float d32 = value.M41 * a5 + value.M43 * a2 + value.M44 * a1;
            float d33 = value.M41 * -a4 + value.M42 * a2 + value.M44 * a0;
            float d34 = value.M41 * a3 + value.M42 * -a1 + value.M43 * a0;

            float d41 = value.M32 * a5 + value.M33 * a4 + value.M34 * a3;
            float d42 = value.M31 * a5 + value.M33 * a2 + value.M34 * a1;
            float d43 = value.M31 * -a4 + value.M32 * a2 + value.M34 * a0;
            float d44 = value.M31 * a3 + value.M32 * -a1 + value.M33 * a0;

            result.M11 = +d11 * det; result.M12 = -d21 * det; result.M13 = +d31 * det; result.M14 = -d41 * det;
            result.M21 = -d12 * det; result.M22 = +d22 * det; result.M23 = -d32 * det; result.M24 = +d42 * det;
            result.M31 = +d13 * det; result.M32 = -d23 * det; result.M33 = +d33 * det; result.M34 = -d43 * det;
            result.M41 = -d14 * det; result.M42 = +d24 * det; result.M43 = -d34 * det; result.M44 = +d44 * det;
        }

        public static Matrix Invert(Matrix value)
        {
            Matrix result;
            Matrix.Invert(ref value, out result);
            return result;
        }

        public static void CreateBillboard(ref Vector3 objectPosition,
                                           ref Vector3 cameraPosition,
                                           ref Vector3 cameraUpVector,
                                           ref Vector3 cameraForwardVector,
                                           out Matrix result)
        {
            Vector3 crossed;
            Vector3 final;
            Vector3 difference = objectPosition - cameraPosition;

            float lengthSq = difference.LengthSquared();
            if (lengthSq < MathHelper.ZeroTolerance)
                difference = -cameraForwardVector;
            else
                difference *= (float) (1.0 / Math.Sqrt(lengthSq));

            Vector3.Cross(ref cameraUpVector, ref difference, out crossed);
            crossed.Normalize();
            Vector3.Cross(ref difference, ref crossed, out final);

            result.M11 = crossed.X;
            result.M12 = crossed.Y;
            result.M13 = crossed.Z;
            result.M14 = 0.0f;
            result.M21 = final.X;
            result.M22 = final.Y;
            result.M23 = final.Z;
            result.M24 = 0.0f;
            result.M31 = difference.X;
            result.M32 = difference.Y;
            result.M33 = difference.Z;
            result.M34 = 0.0f;
            result.M41 = objectPosition.X;
            result.M42 = objectPosition.Y;
            result.M43 = objectPosition.Z;
            result.M44 = 1.0f;
        }

        public static Matrix CreateBillboard(Vector3 objectPosition,
                                             Vector3 cameraPosition,
                                             Vector3 cameraUpVector,
                                             Vector3 cameraForwardVector)
        {
            Matrix result;
            CreateBillboard(ref objectPosition,
                            ref cameraPosition,
                            ref cameraUpVector,
                            ref cameraForwardVector,
                            out result);
            return result;
        }
    
        public static void CreateLookAt(ref Vector3 cameraPosition,
                                        ref Vector3 cameraTarget,
                                        ref Vector3 cameraUpVector,
                                        out Matrix result)
        {
            Vector3 xaxis, yaxis, zaxis;
            Vector3.Subtract(ref cameraPosition, ref cameraTarget, out zaxis);
            zaxis.Normalize();
            Vector3.Cross(ref cameraUpVector, ref zaxis, out xaxis);
            xaxis.Normalize();
            Vector3.Cross(ref zaxis, ref xaxis, out yaxis);

            result = Matrix.Identity;
            result.M11 = xaxis.X; result.M21 = xaxis.Y; result.M31 = xaxis.Z;
            result.M12 = yaxis.X; result.M22 = yaxis.Y; result.M32 = yaxis.Z;
            result.M13 = zaxis.X; result.M23 = zaxis.Y; result.M33 = zaxis.Z;

            Vector3.Dot(ref xaxis, ref cameraPosition, out result.M41);
            Vector3.Dot(ref yaxis, ref cameraPosition, out result.M42);
            Vector3.Dot(ref zaxis, ref cameraPosition, out result.M43);

            result.M41 = -result.M41;
            result.M42 = -result.M42;
            result.M43 = -result.M43;
        }

        public static Matrix CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            Matrix result;
            CreateLookAt(ref cameraPosition, ref cameraTarget, ref cameraUpVector, out result);
            return result;
        }

        public static void CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane, out Matrix result)
        {
            float zRange = 1.0f / (zNearPlane - zFarPlane);
            result = new Matrix
            {
                M11 = 2.0f / width,
                M22 = 2.0f / height,
                M33 = zRange,
                M43 = zNearPlane * zRange,
                M44 = 1.0f
            };
        }

        public static Matrix CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane)
        {
            Matrix result;
            CreateOrthographic(width, height, zNearPlane, zFarPlane, out result);
            return result;
        }

        public static void CreateOrthographicOffCenter(float left, float right, float bottom, float top,
                                                       float zNearPlane, float zFarPlane, out Matrix result)
        {
            float zRange = 1.0f / (zNearPlane - zFarPlane);
            result = new Matrix
            {
                M11 = 2.0f / (right - left),
                M22 = 2.0f / (top - bottom),
                M33 = zRange,
                M41 = (left + right) / (left - right),
                M42 = (top + bottom) / (bottom - top),
                M43 = zNearPlane * zRange,
                M44 = 1.0f
            };
        }

        public static Matrix CreateOrthographicOffCenter(float left, float right, float bottom, float top,
                                                         float zNearPlane, float zFarPlane)
        {
            Matrix result;
            CreateOrthographicOffCenter(left, right, bottom, top, zNearPlane, zFarPlane, out result);
            return result;
        }

        public static void CreatePerspective(float width, float height,
                                             float nearPlaneDistance, float farPlaneDistance,
                                             out Matrix result)
        {
            float zRange = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            result = new Matrix
            {
                M11 = (2.0f * nearPlaneDistance) / width,
                M22 = (2.0f * nearPlaneDistance) / height,
                M33 = zRange,
                M34 = -1.0f,
                M43 = nearPlaneDistance * zRange
            };
        }

        public static Matrix CreatePerspective(float width, float height,
                                               float nearPlaneDistance, float farPlaneDistance)
        {
            Matrix result;
            CreatePerspective(width, height, nearPlaneDistance, farPlaneDistance, out result);
            return result;
        }

        public static void CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio,
                                                        float nearPlaneDistance, float farPlaneDistance,
                                                        out Matrix result)
        {
            float yScale = (float) (1.0f / Math.Tan(fieldOfView * 0.5f));
            float xScale = yScale / aspectRatio;
            float zRange = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            result = new Matrix
            {
                M11 = xScale,
                M22 = yScale,
                M33 = zRange,
                M34 = -1.0f,
                M43 = nearPlaneDistance * zRange
            };
        }

        public static Matrix CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio,
                                                          float nearPlaneDistance, float farPlaneDistance)
        {
            Matrix result;
            CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlaneDistance, farPlaneDistance, out result);
            return result;
        }

        public static void CreatePerspectiveOffCenter(float left, float right, float bottom, float top,
                                                      float nearPlaneDistance, float farPlaneDistance,
                                                      out Matrix result)
        {
            float zRange = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            result = new Matrix
            {
                M11 = 2.0f * nearPlaneDistance / (right - left),
                M22 = 2.0f * nearPlaneDistance / (top - bottom),
                M31 = (left + right) / (right - left),
                M32 = (top + bottom) / (top - bottom),
                M33 = zRange,
                M34 = -1.0f,
                M43 = nearPlaneDistance * zRange
            };
        }

        public static Matrix CreatePerspectiveOffCenter(float left, float right, float bottom, float top,
                                                        float nearPlaneDistance, float farPlaneDistance)
        {
            Matrix result;
            CreatePerspectiveOffCenter(left, right, bottom, top, nearPlaneDistance, farPlaneDistance, out result);
            return result;
        }

        public static void CreateReflection(ref Plane plane, out Matrix result)
        {
            float x = plane.Normal.X;
            float y = plane.Normal.Y;
            float z = plane.Normal.Z;
            float x2 = -2.0f * x;
            float y2 = -2.0f * y;
            float z2 = -2.0f * z;

            result.M11 = (x2 * x) + 1.0f;
            result.M12 = y2 * x;
            result.M13 = z2 * x;
            result.M14 = 0.0f;
            result.M21 = x2 * y;
            result.M22 = (y2 * y) + 1.0f;
            result.M23 = z2 * y;
            result.M24 = 0.0f;
            result.M31 = x2 * z;
            result.M32 = y2 * z;
            result.M33 = (z2 * z) + 1.0f;
            result.M34 = 0.0f;
            result.M41 = x2 * plane.D;
            result.M42 = y2 * plane.D;
            result.M43 = z2 * plane.D;
            result.M44 = 1.0f;
        }

        public static Matrix CreateReflection(Plane plane)
        {
            Matrix result;
            CreateReflection(ref plane, out result);
            return result;
        }

        public static void CreateShadow(ref Vector4 light, ref Plane plane, out Matrix result)
        {
            float dot = (plane.Normal.X * light.X) + (plane.Normal.Y * light.Y) + (plane.Normal.Z * light.Z) + (plane.D * light.W);
            float x = -plane.Normal.X;
            float y = -plane.Normal.Y;
            float z = -plane.Normal.Z;
            float d = -plane.D;

            result.M11 = (x * light.X) + dot;
            result.M21 = y * light.X;
            result.M31 = z * light.X;
            result.M41 = d * light.X;
            result.M12 = x * light.Y;
            result.M22 = (y * light.Y) + dot;
            result.M32 = z * light.Y;
            result.M42 = d * light.Y;
            result.M13 = x * light.Z;
            result.M23 = y * light.Z;
            result.M33 = (z * light.Z) + dot;
            result.M43 = d * light.Z;
            result.M14 = x * light.W;
            result.M24 = y * light.W;
            result.M34 = z * light.W;
            result.M44 = (d * light.W) + dot;
        }

        public static Matrix CreateShadow(Vector4 light, Plane plane)
        {
            Matrix result;
            CreateShadow(ref light, ref plane, out result);
            return result;
        }

        public static void CreateScale(ref Vector3 scale, out Matrix result)
        {
            CreateScale(scale.X, scale.Y, scale.Z, out result);
        }

        public static Matrix CreateScale(Vector3 scale)
        {
            Matrix result;
            CreateScale(ref scale, out result);
            return result;
        }

        public static void CreateScale(float x, float y, float z, out Matrix result)
        {
            result = Matrix.Identity;
            result.M11 = x;
            result.M22 = y;
            result.M33 = z;
        }

        public static Matrix CreateScale(float x, float y, float z)
        {
            Matrix result;
            CreateScale(x, y, z, out result);
            return result;
        }

        public static void CreateScale(float scale, out Matrix result)
        {
            result = Matrix.Identity;
            result.M11 = result.M22 = result.M33 = scale;
        }

        public static Matrix CreateScale(float scale)
        {
            Matrix result;
            CreateScale(scale, out result);
            return result;
        }

        public static void CreateRotationX(float radians, out Matrix result)
        {
            float cos = (float) Math.Cos(radians);
            float sin = (float) Math.Sin(radians);

            result = Matrix.Identity;
            result.M22 = cos;
            result.M23 = sin;
            result.M32 = -sin;
            result.M33 = cos;
        }

        public static Matrix CreateRotationX(float radians)
        {
            Matrix result;
            CreateRotationX(radians, out result);
            return result;
        }

        public static void CreateRotationY(float radians, out Matrix result)
        {
            float cos = (float) Math.Cos(radians);
            float sin = (float) Math.Sin(radians);

            result = Matrix.Identity;
            result.M11 = cos;
            result.M13 = -sin;
            result.M31 = sin;
            result.M33 = cos;
        }

        public static Matrix CreateRotationY(float radians)
        {
            Matrix result;
            CreateRotationY(radians, out result);
            return result;
        }

        public static void CreateRotationZ(float radians, out Matrix result)
        {
            float cos = (float) Math.Cos(radians);
            float sin = (float) Math.Sin(radians);

            result = Matrix.Identity;
            result.M11 = cos;
            result.M12 = sin;
            result.M21 = -sin;
            result.M22 = cos;
        }

        public static Matrix CreateRotationZ(float radians)
        {
            Matrix result;
            CreateRotationZ(radians, out result);
            return result;
        }

        public static void CreateFromAxisAngle(ref Vector3 axis, float radians, out Matrix result)
        {
            float x = axis.X;
            float y = axis.Y;
            float z = axis.Z;
            float cos = (float) Math.Cos(radians);
            float sin = (float) Math.Sin(radians);
            float xx = x * x;
            float yy = y * y;
            float zz = z * z;
            float xy = x * y;
            float xz = x * z;
            float yz = y * z;

            result = Matrix.Identity;
            result.M11 = xx + (cos * (1.0f - xx));
            result.M12 = (xy - (cos * xy)) + (sin * z);
            result.M13 = (xz - (cos * xz)) - (sin * y);
            result.M21 = (xy - (cos * xy)) - (sin * z);
            result.M22 = yy + (cos * (1.0f - yy));
            result.M23 = (yz - (cos * yz)) + (sin * x);
            result.M31 = (xz - (cos * xz)) + (sin * y);
            result.M32 = (yz - (cos * yz)) - (sin * x);
            result.M33 = zz + (cos * (1.0f - zz));
        }

        public static Matrix CreateFromAxisAngle(Vector3 axis, float radians)
        {
            Matrix result;
            CreateFromAxisAngle(ref axis, radians, out result);
            return result;
        }

        public static void CreateFromQuaternion(ref Quaternion quaternion, out Matrix result)
        {
            float xx = quaternion.X * quaternion.X;
            float yy = quaternion.Y * quaternion.Y;
            float zz = quaternion.Z * quaternion.Z;
            float xy = quaternion.X * quaternion.Y;
            float zw = quaternion.Z * quaternion.W;
            float zx = quaternion.Z * quaternion.X;
            float yw = quaternion.Y * quaternion.W;
            float yz = quaternion.Y * quaternion.Z;
            float xw = quaternion.X * quaternion.W;

            result = Matrix.Identity;
            result.M11 = 1.0f - (2.0f * (yy + zz));
            result.M12 = 2.0f * (xy + zw);
            result.M13 = 2.0f * (zx - yw);
            result.M21 = 2.0f * (xy - zw);
            result.M22 = 1.0f - (2.0f * (zz + xx));
            result.M23 = 2.0f * (yz + xw);
            result.M31 = 2.0f * (zx + yw);
            result.M32 = 2.0f * (yz - xw);
            result.M33 = 1.0f - (2.0f * (yy + xx));
        }

        public static Matrix CreateFromQuaternion(Quaternion quaternion)
        {
            Matrix result;
            CreateFromQuaternion(ref quaternion, out result);
            return result;
        }

        public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Matrix result)
        {
            Quaternion quaternion = new Quaternion();
            Quaternion.RotationYawPitchRoll(yaw, pitch, roll, out quaternion);
            CreateFromQuaternion(ref quaternion, out result);
        }

        public static Matrix CreateFromYawPitchRoll(float yaw, float pitch, float roll)
        {
            Matrix result;
            CreateFromYawPitchRoll(yaw, pitch, roll, out result);
            return result;
        }

        public static void CreateTranslation(ref Vector3 value, out Matrix result)
        {
            CreateTranslation(value.X, value.Y, value.Z, out result);
        }

        public static Matrix CreateTranslation(Vector3 value)
        {
            Matrix result;
            CreateTranslation(ref value, out result);
            return result;
        }

        public static void CreateTranslation(float x, float y, float z, out Matrix result)
        {
            result = Matrix.Identity;
            result.M41 = x;
            result.M42 = y;
            result.M43 = z;
        }

        public static Matrix CreateTranslation(float x, float y, float z)
        {
            Matrix result;
            CreateTranslation(x, y, z, out result);
            return result;
        }

        #region operator

        public static Matrix operator +(Matrix left, Matrix right)
        {
            Matrix result;
            Add(ref left, ref right, out result);
            return result;
        }

        public static Matrix operator -(Matrix left, Matrix right)
        {
            Matrix result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        public static Matrix operator -(Matrix value)
        {
            Matrix result;
            Negate(ref value, out result);
            return result;
        }

        public static Matrix operator *(float left, Matrix right)
        {
            Matrix result;
            Multiply(ref right, left, out result);
            return result;
        }

        public static Matrix operator *(Matrix left, float right)
        {
            Matrix result;
            Multiply(ref left, right, out result);
            return result;
        }

        public static Matrix operator *(Matrix left, Matrix right)
        {
            Matrix result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        public static Matrix operator /(Matrix left, float right)
        {
            Matrix result;
            Divide(ref left, right, out result);
            return result;
        }

        public static Matrix operator /(Matrix left, Matrix right)
        {
            Matrix result;
            Divide(ref left, ref right, out result);
            return result;
        }

        #endregion

        #region IEquatable

        public static bool operator ==(Matrix left, Matrix right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Matrix left, Matrix right)
        {
            return !left.Equals(right);
        }
        
        public bool Equals(Matrix other)
        {
            return (Math.Abs(other.M11 - M11) < MathHelper.ZeroTolerance &&
                Math.Abs(other.M12 - M12) < MathHelper.ZeroTolerance &&
                Math.Abs(other.M13 - M13) < MathHelper.ZeroTolerance &&
                Math.Abs(other.M14 - M14) < MathHelper.ZeroTolerance &&

                Math.Abs(other.M21 - M21) < MathHelper.ZeroTolerance &&
                Math.Abs(other.M22 - M22) < MathHelper.ZeroTolerance &&
                Math.Abs(other.M23 - M23) < MathHelper.ZeroTolerance &&
                Math.Abs(other.M24 - M24) < MathHelper.ZeroTolerance &&

                Math.Abs(other.M31 - M31) < MathHelper.ZeroTolerance &&
                Math.Abs(other.M32 - M32) < MathHelper.ZeroTolerance &&
                Math.Abs(other.M33 - M33) < MathHelper.ZeroTolerance &&
                Math.Abs(other.M34 - M34) < MathHelper.ZeroTolerance &&

                Math.Abs(other.M41 - M41) < MathHelper.ZeroTolerance &&
                Math.Abs(other.M42 - M42) < MathHelper.ZeroTolerance &&
                Math.Abs(other.M43 - M43) < MathHelper.ZeroTolerance &&
                Math.Abs(other.M44 - M44) < MathHelper.ZeroTolerance);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((Matrix) obj);
        }

        public override int GetHashCode()
        {
            return M11.GetHashCode() ^ M12.GetHashCode() ^ M13.GetHashCode() ^ M14.GetHashCode() ^
               M21.GetHashCode() ^ M22.GetHashCode() ^ M23.GetHashCode() ^ M24.GetHashCode() ^
               M31.GetHashCode() ^ M32.GetHashCode() ^ M33.GetHashCode() ^ M34.GetHashCode() ^
               M41.GetHashCode() ^ M42.GetHashCode() ^ M43.GetHashCode() ^ M44.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{" +
                "{M11:" + M11 + " M12:" + M12 + " M13:" + M13 + " M14:" + M14 + "}" +
                "{M21:" + M21 + " M22:" + M22 + " M23:" + M23 + " M24:" + M24 + "}" +
                "{M31:" + M31 + " M32:" + M32 + " M33:" + M33 + " M34:" + M34 + "}" +
                "{M41:" + M41 + " M42:" + M42 + " M43:" + M43 + " M44:" + M44 + "}" +
                "}}";
        }

        #endregion
    }
}
