#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public interface IEffect
    {
        void Apply(DeviceContext context);
    }
}
