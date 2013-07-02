#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public interface IEffect
    {
        DeviceContext DeviceContext { get; }

        void Apply();
    }
}
