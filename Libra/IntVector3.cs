#region Using

using System;

#endregion

namespace Libra
{
    [Serializable]
    public struct IntVector3 : IEquatable<IntVector3>
    {
        public static readonly IntVector3 Zero = new IntVector3();

        public static readonly IntVector3 One = new IntVector3(1);

        public static readonly IntVector3 Up = new IntVector3(0, 1, 0);

        public static readonly IntVector3 Down = new IntVector3(0, -1, 0);

        public static readonly IntVector3 Forward = new IntVector3(0, 0, 1);

        public static readonly IntVector3 Backward = new IntVector3(0, 0, -1);

        public static readonly IntVector3 Left = new IntVector3(-1, 0, 0);

        public static readonly IntVector3 Right = new IntVector3(1, 0, 0);

        public int X;

        public int Y;

        public int Z;

        public IntVector3(int value)
        {
            X = value;
            Y = value;
            Z = value;
        }

        public IntVector3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static IntVector3 Negate(IntVector3 value)
        {
            IntVector3 result;
            Negate(ref value, out result);
            return result;
        }

        public static void Negate(ref IntVector3 value, out IntVector3 result)
        {
            result = new IntVector3
            {
                X = -value.X,
                Y = -value.Y,
                Z = -value.Z
            };
        }

        public static IntVector3 Add(IntVector3 value1, IntVector3 value2)
        {
            IntVector3 result;
            Add(ref value1, ref value2, out result);
            return result;
        }

        public static void Add(ref IntVector3 value1, ref IntVector3 value2, out IntVector3 result)
        {
            result = new IntVector3
            {
                X = value1.X + value2.X,
                Y = value1.Y + value2.Y,
                Z = value1.Z + value2.Z
            };
        }

        public static IntVector3 Subtract(IntVector3 value1, IntVector3 value2)
        {
            IntVector3 result;
            Subtract(ref value1, ref value2, out result);
            return result;
        }

        public static void Subtract(ref IntVector3 value1, ref IntVector3 value2, out IntVector3 result)
        {
            result = new IntVector3
            {
                X = value1.X - value2.X,
                Y = value1.Y - value2.Y,
                Z = value1.Z - value2.Z
            };
        }

        public static IntVector3 Multiply(IntVector3 value1, IntVector3 value2)
        {
            IntVector3 result;
            Multiply(ref value1, ref value1, out result);
            return result;
        }

        public static void Multiply(ref IntVector3 value1, ref IntVector3 value2, out IntVector3 result)
        {
            result = new IntVector3
            {
                X = value1.X * value2.X,
                Y = value1.Y * value2.Y,
                Z = value1.Z * value2.Z
            };
        }

        public static IntVector3 Multiply(IntVector3 value, int scaleFactor)
        {
            IntVector3 result;
            Multiply(ref value, scaleFactor, out result);
            return result;
        }

        public static void Multiply(ref IntVector3 value, int scaleFactor, out IntVector3 result)
        {
            result = new IntVector3
            {
                X = value.X * scaleFactor,
                Y = value.Y * scaleFactor,
                Z = value.Z * scaleFactor
            };
        }

        public static IntVector3 Divide(IntVector3 value1, IntVector3 value2)
        {
            IntVector3 result;
            Divide(ref value1, ref value2, out result);
            return result;
        }

        public static void Divide(ref IntVector3 value1, ref IntVector3 value2, out IntVector3 result)
        {
            result = new IntVector3
            {
                X = value1.X / value2.X,
                Y = value1.Y / value2.Y,
                Z = value1.Z / value2.Z
            };
        }

        public static IntVector3 Divide(IntVector3 value, int divider)
        {
            IntVector3 result;
            Divide(ref value, divider, out result);
            return result;
        }

        public static void Divide(ref IntVector3 value, int divider, out IntVector3 result)
        {
            result = new IntVector3
            {
                X = value.X / divider,
                Y = value.Y / divider,
                Z = value.Z / divider
            };
        }

        public static void Distance(ref IntVector3 v1, ref IntVector3 v2, out float result)
        {
            int distanceSquared;
            DistanceSquared(ref v1, ref v2, out distanceSquared);
            result = (float) Math.Sqrt(distanceSquared);
        }

        public static void DistanceSquared(ref IntVector3 v1, ref IntVector3 v2, out int result)
        {
            var x = v2.X - v1.X;
            var y = v2.Y - v1.Y;
            var z = v2.Z - v1.Z;
            result = x * x + y * y + z * z;
        }

        public float Length()
        {
            return (float) Math.Sqrt(LengthSquared());
        }

        public int LengthSquared()
        {
            return X * X + Y * Y + Z * Z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        #region Operators

        public static IntVector3 operator +(IntVector3 value1, IntVector3 value2)
        {
            IntVector3 result;
            Add(ref value1, ref value2, out result);
            return result;
        }

        public static IntVector3 operator -(IntVector3 value)
        {
            IntVector3 result;
            Negate(ref value, out result);
            return result;
        }

        public static IntVector3 operator -(IntVector3 value1, IntVector3 value2)
        {
            IntVector3 result;
            Subtract(ref value1, ref value2, out result);
            return result;
        }

        public static IntVector3 operator *(IntVector3 value1, IntVector3 value2)
        {
            IntVector3 result;
            Multiply(ref value1, ref value2, out result);
            return result;
        }

        public static IntVector3 operator *(int scaleFactor, IntVector3 value)
        {
            IntVector3 result;
            Multiply(ref value, scaleFactor, out result);
            return result;
        }

        public static IntVector3 operator *(IntVector3 value, int scaleFactor)
        {
            IntVector3 result;
            Multiply(ref value, scaleFactor, out result);
            return result;
        }

        public static IntVector3 operator /(IntVector3 value1, IntVector3 value2)
        {
            IntVector3 result;
            Divide(ref value1, ref value2, out result);
            return result;
        }

        public static IntVector3 operator /(IntVector3 value, int divider)
        {
            IntVector3 result;
            Divide(ref value, divider, out result);
            return result;
        }

        #endregion

        #region Equatable

        public static bool operator ==(IntVector3 value1, IntVector3 value2)
        {
            return value1.Equals(value2);
        }

        public static bool operator !=(IntVector3 value1, IntVector3 value2)
        {
            return !value1.Equals(value2);
        }

        public bool Equals(IntVector3 other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((IntVector3) obj);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{X:" + X + " Y:" + Y + " Z:" + Z + "}";
        }

        #endregion
    }
}
