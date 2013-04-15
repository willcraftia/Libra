#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class BlurSettings
    {
        public const int DefaultRadius = 1;

        public const float DefaultAmount = 1;

        int radius = DefaultRadius;

        float amount = DefaultAmount;

        /// <summary>
        /// 適用半径を取得または設定します。
        /// </summary>
        public int Radius
        {
            get { return radius; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                radius = value;
            }
        }

        /// <summary>
        /// 適用量を取得または設定します。
        /// </summary>
        public float Amount
        {
            get { return amount; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("value");

                amount = value;
            }
        }
    }
}
