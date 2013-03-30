#region Using

using System;

#endregion

namespace Libra.Input
{
    public interface IKeyboard
    {
        KeyboardState GetState();
    }
}
