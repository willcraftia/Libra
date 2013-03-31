#region Using

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using DXGIFactory1 = SharpDX.DXGI.Factory1;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxGraphicsFactory : GraphicsFactory
    {
        ReadOnlyCollection<IAdapter> adapters;

        IAdapter defaultAdapter;

        public override ReadOnlyCollection<IAdapter> Adapters
        {
            get { return adapters; }
        }

        public override IAdapter DefaultAdapter
        {
            get { return defaultAdapter; }
        }

        public SdxGraphicsFactory()
        {
            using (var factory = new DXGIFactory1())
            {
                var count = factory.GetAdapterCount1();
                var adapterList = new List<IAdapter>(count);

                for (int i = 0; i < count; i++)
                {
                    var isDefaultAdapter = (i == 0);
                    var adapter = new SdxAdapter(factory.GetAdapter1(i), isDefaultAdapter);
                    adapterList.Add(adapter);
                }

                defaultAdapter = adapterList[0];
                adapters = new ReadOnlyCollection<IAdapter>(adapterList);
            }
        }

        public override IDevice CreateDevice(IAdapter adapter, DeviceSettings settings, DeviceProfile[] profiles)
        {
            return new SdxDevice(adapter as SdxAdapter, settings, profiles);
        }

        public override SwapChain CreateSwapChain(IDevice device, SwapChainSettings settings)
        {
            return new SdxSwapChain(device as SdxDevice, settings);
        }
    }
}
