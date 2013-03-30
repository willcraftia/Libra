#region Using

using System;

#endregion

namespace Libra.Graphics
{
    [FlagsAttribute]
    public enum ColorWriteChannels
    {
        None    = 0,
        Red     = 1,
        Green   = 2,
        Blue    = 4,
        Alpha   = 8,
        All     = 15,
    }
}
