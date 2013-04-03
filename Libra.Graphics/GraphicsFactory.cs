#region Using

using System;
using System.Collections.ObjectModel;
using System.Configuration;

#endregion

namespace Libra.Graphics
{
    public abstract class GraphicsFactory
    {
        public const string AppSettingKey = "Libra.Graphics.GraphicsFactory";

        const string DefaultImplementation = "Libra.Graphics.SharpDX.SdxGraphicsFactory, Libra.Graphics.SharpDX, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        public abstract ReadOnlyCollection<IAdapter> Adapters { get; }

        public abstract IAdapter DefaultAdapter { get; }

        public abstract Device CreateDevice(IAdapter adapter, DeviceSettings settings, DeviceProfile[] profiles);

        public abstract SwapChain CreateSwapChain(Device device, SwapChainSettings settings);

        public static GraphicsFactory CreateGraphicsFactory()
        {
            // app.config 定義を参照。
            var implementation = ConfigurationManager.AppSettings[AppSettingKey];

            // app.config で未定義ならば SharpDX 実装をデフォルト指定。
            if (implementation == null)
                implementation = DefaultImplementation;

            var type = Type.GetType(implementation);
            return Activator.CreateInstance(type) as GraphicsFactory;
        }
    }
}
