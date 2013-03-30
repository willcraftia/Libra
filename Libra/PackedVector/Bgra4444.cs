#region Using

using System;

#endregion

namespace Libra.PackedVector
{
    public struct Bgra4444 : IPackedVector<ushort>, IEquatable<Bgra4444>
    {
        // 各要素 4 ビット。
        const float Scale = (float) 0xf;

        ushort packedValue;

        public ushort PackedValue
        {
            get { return packedValue; }
            set { packedValue = value; }
        }

        public Bgra4444(Vector4 vector)
        {
            packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
        }

        public Bgra4444(float x, float y, float z, float w)
        {
            packedValue = Pack(x, y, z, w);
        }

        public void PackFromVector4(Vector4 vector)
        {
            packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
        }

        public Vector4 ToVector4()
        {
            // 恐らく XNA と等価。
            // Scale で除算した場合の精度が完全に一致しているかは不明。

            int a = (packedValue & 0xf000) >> 12;
            int r = (packedValue & 0x0f00) >> 8;
            int g = (packedValue & 0x00f0) >> 4;
            int b = (packedValue & 0x000f);

            float z = b / Scale;
            float y = g / Scale;
            float x = r / Scale;
            float w = a / Scale;

            return new Vector4(x, y, z, w);
        }

        static ushort Pack(float x, float y, float z, float w)
        {
            int r = Normalize(x);
            int g = Normalize(y);
            int b = Normalize(z);
            int a = Normalize(w);

            return (ushort) (b | (g << 4) | (r << 8) | (a << 12));
        }

        static int Normalize(float value)
        {
            // 0.0f - 0.1f を 0x0 - 0xf へ正規化。

            // 恐らく XNA と等価。
            // Math.Round を使わないと XNA とは異なる値となる。
            // また、Clamp も必要 (正規化の前か後かは知らないが等価)。
            // 恐らく、DXTK の MakeSpriteFont にある Bgra4444 正規化はロジックの誤り。

            return (int) MathHelper.Clamp((float) Math.Round(value * Scale), 0, Scale);
        }

        #region IEquatable

        public static bool operator ==(Bgra4444 left, Bgra4444 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Bgra4444 left, Bgra4444 right)
        {
            return !left.Equals(right);
        }

        public bool Equals(Bgra4444 other)
        {
            return packedValue == other.packedValue;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((Bgra4444) obj);
        }

        public override int GetHashCode()
        {
            return packedValue.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{" + packedValue.ToString("X4") + "}";
        }

        #endregion
    }
}
