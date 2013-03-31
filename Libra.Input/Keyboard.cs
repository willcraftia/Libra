#region Using

using System;

#endregion

namespace Libra.Input
{
    public static class Keyboard
    {
        static IKeyboard keyboard;

        public static void Initialize(IKeyboard keyboard)
        {
            Keyboard.keyboard = keyboard;
        }

        public static KeyboardState GetState()
        {
            if (keyboard == null) return new KeyboardState();

            return keyboard.GetState();
        }
    }
}
