#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public struct Box
    {
        public int Left;

        public int Top;

        public int Front;

        public int Right;

        public int Bottom;

        public int Back;

        public Box(int left, int top, int front, int right, int bottom, int back)
        {
            Left = left;
            Top = top;
            Front = front;
            Right = right;
            Bottom = bottom;
            Back = back;
        }
    }
}
