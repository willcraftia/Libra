#region Using

using System;

#endregion

namespace Libra.Input.Forms
{
    public sealed class FormKeyboard : IKeyboard
    {
        public static FormKeyboard Instance = new FormKeyboard();

        public KeyboardState State;

        FormKeyboard() { }

        public KeyboardState GetState()
        {
            return State;
        }
    }
}
