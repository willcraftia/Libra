#region Using

using System;

#endregion

namespace Libra.Input.Forms
{
    public sealed class FormKeyboard : IKeyboard
    {
        internal KeyboardState State;

        public KeyboardState GetState()
        {
            return State;
        }
    }
}
