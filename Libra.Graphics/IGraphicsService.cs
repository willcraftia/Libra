#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public interface IGraphicsService
    {
        event EventHandler DeviceDisposing;

        Device Device { get; }

        SwapChain SwapChain { get; }
    }
}
