#region Using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using DXGIFactory1 = SharpDX.DXGI.Factory1;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxGraphicsFactory : IGraphicsFactory
    {
        public ReadOnlyCollection<IAdapter> Adapters { get; private set; }

        public IAdapter DefaultAdapter { get; private set; }

        public SdxGraphicsFactory()
        {
            using (var factory = new DXGIFactory1())
            {
                var count = factory.GetAdapterCount1();
                var adapters = new List<IAdapter>(count);

                for (int i = 0; i < count; i++)
                {
                    var isDefaultAdapter = (i == 0);
                    var adapter = new SdxAdapter(factory.GetAdapter1(i), isDefaultAdapter);
                    adapters.Add(adapter);
                }

                DefaultAdapter = adapters[0];

                Adapters = new ReadOnlyCollection<IAdapter>(adapters);
            }
        }

        public IDevice CreateDevice(IAdapter adapter, DeviceSettings settings, DeviceProfile[] profiles)
        {
            return new SdxDevice(adapter as SdxAdapter, settings, profiles);
        }

        public SwapChain CreateSwapChain(IDevice device, SwapChainSettings settings)
        {
            return new SdxSwapChain(device as SdxDevice, settings);
        }
    }
}
