#region Using

using System;

#endregion

namespace Libra
{
    /// <summary>
    /// 整数による境界ボックスです。
    /// </summary>
    [Serializable]
    public struct IntBoundingBox
    {
        /// <summary>
        /// 最小点。
        /// </summary>
        public IntVector3 Min;

        /// <summary>
        /// 最大点。
        /// </summary>
        public IntVector3 Max;

        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="min">最小点。</param>
        /// <param name="size">最大点。</param>
        public IntBoundingBox(IntVector3 min, IntVector3 max)
        {
            Min = min;
            Max = max;
        }

        public static IntBoundingBox CreateFromCenterExtents(IntVector3 center, IntVector3 extents)
        {
            IntBoundingBox result;
            CreateFromCenterExtents(ref center, ref extents, out result);
            return result;
        }

        public static void CreateFromCenterExtents(ref IntVector3 center, ref IntVector3 extents, out IntBoundingBox result)
        {
            result.Min.X = center.X - extents.X;
            result.Min.Y = center.Y - extents.Y;
            result.Min.Z = center.Z - extents.Z;

            result.Max.X = result.Min.X + extents.X * 2;
            result.Max.Y = result.Min.Y + extents.Y * 2;
            result.Max.Z = result.Min.Z + extents.Z * 2;
        }

        /// <summary>
        /// 指定の点が境界ボックスに含まれるか否かを検査します。
        /// </summary>
        /// <param name="point">点。</param>
        /// <param name="result">
        /// true (点が境界ボックスに含まれる場合)、false (それ以外の場合)。
        /// </param>
        public void Contains(ref IntVector3 point, out bool result)
        {
            if (point.X < Min.X || point.Y < Min.Y || point.Z < Min.Z ||
                Max.X < point.X || Max.Y < point.Y || Max.Y < point.Y)
            {
                result = false;
            }
            else
            {
                result = true;
            }
        }

        /// <summary>
        /// 指定の点が境界ボックスに含まれるか否かを検査します。
        /// </summary>
        /// <param name="point">点。</param>
        /// <returns>
        /// true (点が境界ボックスに含まれる場合)、false (それ以外の場合)。
        /// </returns>
        public bool Contains(ref IntVector3 point)
        {
            bool result;
            Contains(ref point, out result);
            return result;
        }

        /// <summary>
        /// 指定の点が境界ボックスに含まれるか否かを検査します。
        /// </summary>
        /// <param name="point">点。</param>
        /// <returns>
        /// true (点が境界ボックスに含まれる場合)、false (それ以外の場合)。
        /// </returns>
        public bool Contains(IntVector3 point)
        {
            return Contains(ref point);
        }

        #region ToString

        public override string ToString()
        {
            return "{Min:" + Min + " Max:" + Max + "}";
        }

        #endregion
    }
}
