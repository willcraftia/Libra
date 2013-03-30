#region Using

using System;

#endregion

namespace Libra.Input
{
    public interface IMouse
    {
        MouseState GetState();

        void SetPosition(int x, int y);
    }
}
