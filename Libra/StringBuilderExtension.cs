#region Using

using System;
using System.Globalization;
using System.Text;

#endregion

namespace Libra
{
    /// <summary>
    /// StringBuilder の拡張クラスです。
    /// </summary>
    public static class StringBuilderExtension
    {
        // 参考: ひにけに GD - デバッグサンプル
        // http://blogs.msdn.com/b/ito/archive/2008/12/27/debug-components-sample.aspx
        //
        // 上記デバッグ サンプルに含まれる StringBuilderExtensions のうち、
        // カンマ区切りの自動付加機能は不要と判断して削除。
        // また、複数スレッドからの呼び出しを想定し、
        // 数値の文字列化で用いる文字バッファへ ThreadStatic 属性を付加し、
        // スレッド単位での共有としている。

        /// <summary>
        /// 数値の文字列化で用いる文字バッファ。
        /// </summary>
        [ThreadStatic]
        static char[] numberString = new char[32];

        /// <summary>
        /// ボクシングおよび内部メモリ確保を発生させずに int 値を追加します。
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="number"></param>
        /// <param name="showPositiveSign"></param>
        /// <returns></returns>
        public static StringBuilder AppendNumber(this StringBuilder builder, int number, bool showPositiveSign = false)
        {
            return AppendNumberInternal(builder, number, 0, showPositiveSign);
        }

        /// <summary>
        /// ボクシングおよび内部メモリ確保を発生させずに float 値を追加します。
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="number"></param>
        /// <param name="showPositiveSign"></param>
        /// <returns></returns>
        public static StringBuilder AppendNumber(this StringBuilder builder, float number, bool showPositiveSign = false)
        {
            return AppendNumber(builder, number, 2, showPositiveSign);
        }

        /// <summary>
        /// ボクシングおよび内部メモリ確保を発生させずに float 値を追加します。
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="number"></param>
        /// <param name="decimalCount"></param>
        /// <param name="showPositiveSign"></param>
        /// <returns></returns>
        public static StringBuilder AppendNumber(this StringBuilder builder, float number, int decimalCount, bool showPositiveSign = false)
        {
            if (float.IsNaN(number)) return builder.Append("NaN");
            if (float.IsNegativeInfinity(number)) return builder.Append("-Infinity");
            if (float.IsPositiveInfinity(number)) return builder.Append("+Infinity");

            int intNumber = (int) (number * (float) Math.Pow(10, decimalCount) + 0.5f);
            return AppendNumberInternal(builder, intNumber, decimalCount, showPositiveSign);
        }

        static StringBuilder AppendNumberInternal(StringBuilder builder, int number, int decimalCount, bool showPositiveSign)
        {
            var numberFormat = CultureInfo.CurrentCulture.NumberFormat;

            int index = numberString.Length;
            int decimalPos = index - decimalCount;

            if (decimalPos == index)
                decimalPos = index + 1;

            bool isNegative = number < 0;
            number = Math.Abs(number);

            do
            {
                // 小数点。
                if (index == decimalPos)
                {
                    numberString[--index] = numberFormat.NumberDecimalSeparator[0];
                }

                // 数値を文字へ。
                numberString[--index] = (char) ('0' + (number % 10));
                number /= 10;

            } while (number > 0 || decimalPos <= index);


            // 符号の追加。
            if (isNegative)
            {
                numberString[--index] = numberFormat.NegativeSign[0];
            }
            else if (showPositiveSign)
            {
                numberString[--index] = numberFormat.PositiveSign[0];
            }

            // 文字列化数値を追加。
            return builder.Append(numberString, index, numberString.Length - index);
        }
    }
}
