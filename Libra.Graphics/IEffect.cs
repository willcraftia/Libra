#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public interface IEffect : IDisposable
    {
        void Apply(DeviceContext context);
    }
}
