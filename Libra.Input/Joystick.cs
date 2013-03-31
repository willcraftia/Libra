#region Using

using System;

#endregion

namespace Libra.Input
{
    public static class Joystick
    {
        static IJoystick joystick;

        public static void Initialize(IJoystick joystick)
        {
            Joystick.joystick = joystick;
        }

        public static JoystickState GetState()
        {
            if (joystick == null) return new JoystickState();

            return joystick.GetState();
        }
    }
}
