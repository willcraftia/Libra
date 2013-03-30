#region Using

using System;

#endregion

namespace Libra.Graphics
{
    [Flags]
    public enum ClearOptions
    {
        Depth   = 1,
        Stencil = 2,
        Target  = 3
    }
}
