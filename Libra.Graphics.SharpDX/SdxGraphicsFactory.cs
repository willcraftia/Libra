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
        ReadOnlyCollection<Adapter> adapters;

        Adapter defaultAdapter;

        public override ReadOnlyCollection<Adapter> Adapters
        {
            get { return adapters; }
        }

        public override Adapter DefaultAdapter
        {
            get { return defaultAdapter; }
        }

        public SdxGraphicsFactory()
        {
            using (var factory = new DXGIFactory1())
            {
                var count = factory.GetAdapterCount1();
                var adapterList = new List<Adapter>(count);

                for (int i = 0; i < count; i++)
                {
                    var isDefaultAdapter = (i == 0);
                    var adapter = new SdxAdapter(isDefaultAdapter, factory.GetAdapter1(i));
                    adapterList.Add(adapter);
                }

                defaultAdapter = adapterList[0];
                adapters = new ReadOnlyCollection<Adapter>(adapterList);
            }
        }

        public override Device CreateDevice(Adapter adapter, DeviceSettings settings, DeviceProfile[] profiles)
        {
            return new SdxDevice(adapter as SdxAdapter, settings, profiles);
        }

        public override SwapChain CreateSwapChain(Device device, SwapChainSettings settings)
        {
            return new SdxSwapChain(device as SdxDevice, settings);
        }
    }
}
