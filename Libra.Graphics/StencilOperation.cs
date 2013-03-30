#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public enum StencilOperation
    {
        Keep                = 1,
        Zero                = 2,
        Replace             = 3,
        IncrementAndClamp   = 4,
        DecrementAndClamp   = 5,
        Invert              = 6,
        Increment           = 7,
        Decrement           = 8,
    }
}
