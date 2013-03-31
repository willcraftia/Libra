#region Using

using System;

#endregion

namespace Libra.Input
{
    public static class Keyboard
    {
        public static bool Initialized { get; private set; }

        static IKeyboard keyboard;

        public static void Initialize(IKeyboard keyboard)
        {
            if (Initialized) throw new InvalidOperationException("Already initialized.");

            Keyboard.keyboard = keyboard;

            Initialized = true;
        }

        public static KeyboardState GetState()
        {
            if (!Initialized) throw new InvalidOperationException("Not initialized.");
            
            if (keyboard == null) return new KeyboardState();
            return keyboard.GetState();
        }
    }
}
