#region Using

using System;

#endregion

namespace Libra.PackedVector
{
    public sealed class NormalizedByte4 : IPackedVector<uint>, IEquatable<NormalizedByte4>
    {
        const uint Mask = byte.MaxValue;

        const uint NegativeMask = Mask + 1U >> 1;

        const float Scale = (float) (byte.MaxValue >> 1);

        uint packedValue;

        public uint PackedValue
        {
            get { return packedValue; }
            set { packedValue = value; }
        }

        public NormalizedByte4(Vector4 vector)
        {
            packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
        }

        public NormalizedByte4(float x, float y, float z, float w)
        {
            packedValue = Pack(x, y, z, w);
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
            return new Vector4(
                FromSNorm(packedValue),
                FromSNorm(packedValue >> 8),
                FromSNorm(packedValue >> 16),
                FromSNorm(packedValue >> 24));
        }

        static uint Pack(float x, float y, float z, float w)
        {
            return (uint) (ToSNorm(x) | (ToSNorm(y) << 8) | (ToSNorm(z) << 16) | (ToSNorm(w) << 24));
        }

        static uint ToSNorm(float value)
        {
            return (uint) (int) MathHelper.Clamp((float) Math.Round(value * Scale), -Scale, Scale) & 0xff;
        }

        static float FromSNorm(uint value)
        {
            if ((value & Mask) == NegativeMask)
            {
                return -1.0f;
            }

            if ((value & NegativeMask) != 0)
            {
                value |= ~Mask;
            }
            else
            {
                value &= Mask;
            }

            return (float) (int) value / Scale;
        }

        public void PackFromVector4(Vector4 vector)
        {
            packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
        }

        #region IEquatable

        public static bool operator ==(NormalizedByte4 left, NormalizedByte4 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NormalizedByte4 left, NormalizedByte4 right)
        {
            return !left.Equals(right);
        }

        public bool Equals(NormalizedByte4 other)
        {
            return packedValue == other.packedValue;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((NormalizedByte4) obj);
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
