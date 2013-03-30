#region Using

using System;

#endregion

namespace Libra.Input
{
    public struct MouseState
    {
        public int X;

        public int Y;

        public ButtonState LeftButton;

        public ButtonState MiddleButton;

        public ButtonState RightButton;

        public ButtonState XButton1;

        public ButtonState XButton2;

        public int ScrollWheelValue;
    }
}
