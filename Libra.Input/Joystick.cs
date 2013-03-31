#region Using

using System;

#endregion

namespace Libra.Input
{
    public static class Joystick
    {
        public static bool Initialized { get; private set; }

        static IJoystick joystick;

        public static void Initialize(IJoystick joystick)
        {
            if (Initialized) throw new InvalidOperationException("Already initialized.");

            Joystick.joystick = joystick;

            Initialized = true;
        }

        public static JoystickState GetState()
        {
            if (!Initialized) throw new InvalidOperationException("Not initialized.");

            if (joystick == null) return new JoystickState();
            return joystick.GetState();
        }
    }
}
