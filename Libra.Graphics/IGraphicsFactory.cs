#region Using

using System;
using System.Collections.ObjectModel;

#endregion

namespace Libra.Graphics
{
    public interface IGraphicsFactory
    {
        ReadOnlyCollection<IAdapter> Adapters { get; }

        IAdapter DefaultAdapter { get; }

        IDevice CreateDevice(IAdapter adapter, DeviceSettings settings, DeviceProfile[] profiles);

        SwapChain CreateSwapChain(IDevice device, SwapChainSettings settings);
    }
}
