#region Using

using System;

#endregion

namespace Libra.Input
{
    public interface IJoystick
    {
        JoystickState GetState();
    }
}
