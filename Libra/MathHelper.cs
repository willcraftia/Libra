#region Using

using System;

#endregion

namespace Libra
{
    public static class MathHelper
    {
        public const float E = (float) Math.E;

        public const float Log10E = 0.434294f;
        
        public const float Log2E = 1.4427f;

        public const float Pi = (float) Math.PI;

        public const float PiOver2 = (float) (Math.PI / 2.0);

        public const float PiOver4 = (float) (Math.PI / 4.0);

        public const float TwoPi = (float) (Math.PI * 2.0);

        internal const float ZeroTolerance = 1e-6f;

        public static float Barycentric(float value1, float value2, float value3, float amount1, float amount2)
        {
            return value1 + (value2 - value1) * amount1 + (value3 - value1) * amount2;
        }

        public static float Clamp(float value, float min, float max)
        {
            if (max < value) return max;
            if (value < min) return min;
            return value;
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (max < value) return max;
            return value;
        }

        public static float Distance(float value1, float value2)
        {
            return Math.Abs(value1 - value2);
        }

        public static float Lerp(float start, float end, float amount)
        {
            return start + (end - start) * amount;
        }

        public static float SmoothStep(float start, float end, float amount)
        {
            amount = Clamp(amount, 0, 1);
            amount = (amount * amount) * (3.0f - (2.0f * amount));
            return start + ((end - start) * amount);
        }

        public static float Hermite(float value1, float tangent1, float value2, float tangent2, float amount)
        {
            float squared = amount * amount;
            float cubed = squared * amount;
            float part1 = ((2.0f * cubed) - (3.0f * squared)) + 1.0f;
            float part2 = (-2.0f * cubed) + (3.0f * squared);
            float part3 = (cubed - (2.0f * squared)) + amount;
            float part4 = cubed - squared;
            return (((value1 * part1) + (value2 * part2)) + (tangent1 * part3)) + (tangent2 * part4);
        }

        public static float CatmullRom(float value1, float value2, float value3, float value4, float amount)
        {
            float squared = amount * amount;
            float cubed = squared * amount;
            return 0.5f * ((((2.0f * value2) + ((-value1 + value3) * amount)) +
                (((((2.0f * value1) - (5.0f * value2)) + (4.0f * value3)) - value4) * squared)) +
                ((((-value1 + (3.0f * value2)) - (3.0f * value3)) + value4) * cubed));
        }

        public static float Max(float value1, float value2)
        {
            return (value2 < value1) ? value1 : value2;
        }

        public static float Min(float value1, float value2)
        {
            return (value1 < value2) ? value1 : value2;
        }

        public static float ToDegrees(float radian)
        {
            return radian * (180.0f / Pi);
        }

        public static float ToRadians(float degree)
        {
            return degree * (Pi / 180.0f);
        }

        public static float WrapAngle(float angle)
        {
            angle = (float) Math.IEEERemainder((double) angle, 6.2831854820251465);
            if (angle <= -3.141593f)
            {
                angle += 6.283185f;
                return angle;
            }
            if (angle > 3.141593f)
            {
                angle -= 6.283185f;
            }
            return angle;
        }

        public static bool WithinEpsilon(float a, float b)
        {
            float num = a - b;
            return ((-float.Epsilon <= num) && (num <= float.Epsilon));
        }

        /// <summary>
        /// 指定された値が 2 の累乗であるか否かを検査します。
        /// </summary>
        /// <param name="value"></param>
        /// <returns>
        /// true (指定された値が 2 の累乗である場合)、false (それ以外の場合)。
        /// </returns>
        public static bool IsPowerOf2(int value)
        {
            if (value < 1) return false;

            return (value & (value - 1)) == 0;
        }

        /// <summary>
        /// 概算で floor を計算します。
        /// </summary>
        /// <remarks>
        /// 概算であるため Math.Floor より高速ですが、それゆえに Math.Floor とは挙動が異なります。
        /// 例えば、Math.Floor(-1) == -1 であるに対し、MathHelper.FastFloor(-1) == -2 となります。
        /// </remarks>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int FastFloor(float value)
        {
            return 0 <= value ? (int) value : (int) (value - 1);
        }

        /// <summary>
        /// 絶対値を計算します。
        /// </summary>
        /// <remarks>
        /// Math.Abs よりわずかに高速ですが、
        /// int.MaxValue を指定した場合に例外を発生させずに不正な値を返します。
        /// </remarks>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int FastAbs(int value)
        {
            return 0 <= value ? value : -value;
        }

        public static float CalculateGaussian(float sigma, float n)
        {
            // 参考: sigmaRoot = (float) Math.Sqrt(2.0f * Math.PI * sigma * sigma)
            var twoSigmaSquare = 2.0f * sigma * sigma;
            var sigmaRoot = (float) Math.Sqrt(Math.PI * twoSigmaSquare);
            return (float) Math.Exp(-(n * n) / twoSigmaSquare) / sigmaRoot;
        }
    }
}
