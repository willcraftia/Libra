#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public abstract class SharedDeviceResourceBase
    {
        public Device Device { get; private set; }

        protected SharedDeviceResourceBase(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;
        }
    }
}
