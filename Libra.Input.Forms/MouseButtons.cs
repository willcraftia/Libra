#region Using

using System;

#endregion

namespace Libra.Input.Forms
{
    [Flags]
    public enum MouseButtons
    {
        Left    = 1,
        Middle  = 2,
        Right   = 4,
        X1      = 8,
        X2      = 16
    }
}
