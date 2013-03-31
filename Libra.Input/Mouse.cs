#region Using

using System;

#endregion

namespace Libra.Input
{
    public static class Mouse
    {
        static IMouse mouse;

        public static void Initialize(IMouse mouse)
        {
            Mouse.mouse = mouse;
        }

        public static MouseState GetState()
        {
            if (mouse == null) return new MouseState();

            return mouse.GetState();
        }

        public static void SetPosition(int x, int y)
        {
            mouse.SetPosition(x, y);
        }
    }
}
