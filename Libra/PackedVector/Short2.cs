#region Using

using System;

#endregion

namespace Libra.PackedVector
{
    public struct Short2 : IPackedVector<uint>, IEquatable<Short2>
    {
        // Short2 は単に short の範囲にある float を short 化した後に纏めて uint 化するのみ。

        uint packedValue;

        public uint PackedValue
        {
            get { return packedValue; }
            set { packedValue = value; }
        }

        public Short2(Vector2 vector)
        {
            packedValue = Pack(vector.X, vector.Y);
        }

        public Short2(float x, float y)
        {
            packedValue = Pack(x, y);
        }

        public void PackFromVector4(Vector4 vector)
        {
            packedValue = Pack(vector.X, vector.Y);
        }

        public Vector2 ToVector2()
        {
            return new Vector2
            {
                X = (short) (packedValue & 0xffff),
                Y = (short) (packedValue >> 16)
            };
        }

        public Vector4 ToVector4()
        {
            return ToVector2().ToVector4();
        }

        static uint Pack(float x, float y)
        {
            var clamp1 = MathHelper.Clamp(x, short.MinValue, short.MaxValue);
            var clamp2 = MathHelper.Clamp(y, short.MinValue, short.MaxValue);
            var element1 = (uint) ((int) clamp1 & 0xffff);
            var element2 = (uint) ((int) clamp2 & 0xffff);

            return element1 | (element2 << 16);
        }

        #region IEquatable

        public static bool operator ==(Short2 left, Short2 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Short2 left, Short2 right)
        {
            return !left.Equals(right);
        }

        public bool Equals(Short2 other)
        {
            return packedValue == other.packedValue;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((Short2) obj);
        }

        public override int GetHashCode()
        {
            return packedValue.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{" + packedValue.ToString("X8") + "}";
        }

        #endregion
    }
}
