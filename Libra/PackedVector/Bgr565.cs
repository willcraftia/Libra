#region Using

using System;

#endregion

namespace Libra.PackedVector
{
    public struct Bgr565 : IPackedVector<ushort>, IEquatable<Bgr565>
    {
        const int Bit5 = 0x1f;

        const int Bit6 = 0x3f;

        const int RedShift = 11;

        const int GreenShift = 5;

        const int RedMask = (Bit5 << RedShift);

        const int GreenMask = (Bit6 << GreenShift);

        const int BlueMask = Bit5;

        const float Bit5Scale = (float) Bit5;

        const float Bit6Scale = (float) Bit6;

        ushort packedValue;

        public ushort PackedValue
        {
            get { return packedValue; }
            set { packedValue = value; }
        }

        public Bgr565(Vector3 vector)
        {
            packedValue = Pack(vector.X, vector.Y, vector.Z);
        }

        public Bgr565(float x, float y, float z)
        {
            packedValue = Pack(x, y, z);
        }

        public void PackFromVector4(Vector4 vector)
        {
            packedValue = Pack(vector.X, vector.Y, vector.Z);
        }

        public Vector3 ToVector3()
        {
            // 恐らく XNA と等価。
            // Scale で除算した場合の精度が完全に一致しているかは不明。

            int r = (packedValue & RedMask) >> RedShift;
            int g = (packedValue & GreenMask) >> GreenShift;
            int b = (packedValue & BlueMask);

            float z = b / Bit5Scale;
            float y = g / Bit6Scale;
            float x = r / Bit5Scale;

            return new Vector3(x, y, z);
        }

        public Vector4 ToVector4()
        {
            return ToVector3().ToVector4();
        }

        static ushort Pack(float x, float y, float z)
        {
            int r = NormalizeBit5(x);
            int g = NormalizeBit6(y);
            int b = NormalizeBit5(z);

            return (ushort) (b | (g << GreenShift) | (r << RedShift));
        }

        static int NormalizeBit5(float value)
        {
            return (int) MathHelper.Clamp((float) Math.Round(value * Bit5Scale), 0, Bit5Scale);
        }

        static int NormalizeBit6(float value)
        {
            return (int) MathHelper.Clamp((float) Math.Round(value * Bit6Scale), 0, Bit6Scale);
        }

        #region IEquatable

        public static bool operator ==(Bgr565 left, Bgr565 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Bgr565 left, Bgr565 right)
        {
            return !left.Equals(right);
        }

        public bool Equals(Bgr565 other)
        {
            return packedValue == other.packedValue;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((Bgr565) obj);
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
