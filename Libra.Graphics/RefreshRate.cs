#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public struct RefreshRate : IEquatable<RefreshRate>
    {
        public int Numerator;

        public int Denominator;

        public float Value
        {
            get
            {
                return (float) Numerator / (float) Denominator;
            }
        }

        public RefreshRate(int numerator, int denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        #region Equatable

        public static bool operator ==(RefreshRate value1, RefreshRate value2)
        {
            return value1.Equals(value2);
        }

        public static bool operator !=(RefreshRate value1, RefreshRate value2)
        {
            return !value1.Equals(value2);
        }

        public bool Equals(RefreshRate other)
        {
            return Numerator == other.Numerator && Denominator == other.Denominator;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((RefreshRate) obj);
        }

        public override int GetHashCode()
        {
            return Numerator.GetHashCode() ^ Denominator.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return Numerator + "/" + Denominator;
        }

        #endregion
    }
}
