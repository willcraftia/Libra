#region Using

using System;
using System.Runtime.InteropServices;

#endregion

// SharpDX.Rectangle から移植。
// 一部インタフェースを XNA 形式へ変更。
// 一部ロジックを変更。

namespace Libra
{
    [Serializable]
    public struct Rectangle : IEquatable<Rectangle>
    {
        public static readonly Rectangle Empty = new Rectangle();

        public int X;

        public int Y;

        public int Width;

        public int Height;

        public int Left
        {
            get { return X; }
        }

        public int Top
        {
            get { return Y; }
        }

        public int Right
        {
            get { return X + Width; }
        }

        public int Bottom
        {
            get { return Y + Height; }
        }

        public Point Location
        {
            get { return new Point(X, Y); }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public Point Center
        {
            get { return new Point(X + Width / 2, Y + Height / 2); }
        }

        public bool IsEmpty
        {
            get { return X == 0 && Y == 0 && Width == 0 && Height == 0; }
        }

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Contains(int x, int y)
        {
            if (X <= x && x <= Right && Y <= y && y <= Bottom)
            {
                return true;
            }
            return false;
        }

        public bool Contains(Point point)
        {
            bool result;
            Contains(ref point, out result);
            return result;
        }

        public void Contains(ref Point point, out bool result)
        {
            result = (X <= point.X && point.X <= Right && Y <= point.Y && point.Y <= Bottom);
        }

        public bool Contains(Rectangle rectangle)
        {
            bool result;
            Contains(ref rectangle, out result);
            return result;
        }

        public void Contains(ref Rectangle rectangle, out bool result)
        {
            result = (X <= rectangle.X && rectangle.Right <= Right && Y <= rectangle.Y && rectangle.Bottom <= Bottom);
        }

        #region IEquatable

        public static bool operator ==(Rectangle left, Rectangle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rectangle left, Rectangle right)
        {
            return !left.Equals(right);
        }

        public bool Equals(Rectangle other)
        {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((Rectangle) obj);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Width.GetHashCode() ^ Height.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{X:" + X + " Y:" + Y + " Width:" + Width + " Height:" + Height + "}";
        }

        #endregion
    }
}
