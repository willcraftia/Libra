#region Using

using System;

#endregion

namespace Libra
{
    [Serializable]
    public struct Point : IEquatable<Point>
    {
        public static readonly Point Zero = new Point();

        public int X;
        
        public int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        #region IEquatable

        public static bool operator ==(Point left, Point right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Point left, Point right)
        {
            return !left.Equals(right);
        }

        public bool Equals(Point other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((Point) obj);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{X:" + X + " Y:" + Y + "}";
        }

        #endregion
    }
}
