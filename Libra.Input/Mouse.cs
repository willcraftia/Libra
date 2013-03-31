#region Using

using System;

#endregion

namespace Libra.Input
{
    public static class Mouse
    {
        public static bool Initialized { get; private set; }

        static IMouse mouse;

        public static void Initialize(IMouse mouse)
        {
            if (Initialized) throw new InvalidOperationException("Already initialized.");

            Mouse.mouse = mouse;

            Initialized = true;
        }

        public static MouseState GetState()
        {
            if (!Initialized) throw new InvalidOperationException("Not initialized.");
            
            if (mouse == null) return new MouseState();
            return mouse.GetState();
        }

        public static void SetPosition(int x, int y)
        {
            if (!Initialized) throw new InvalidOperationException("Not initialized.");

            mouse.SetPosition(x, y);
        }
    }
}
