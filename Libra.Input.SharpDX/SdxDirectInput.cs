#region Using

using System;

using DIDeviceEnumerationFlags = SharpDX.DirectInput.DeviceEnumerationFlags;
using DIDeviceType = SharpDX.DirectInput.DeviceType;
using DIDirectInput = SharpDX.DirectInput.DirectInput;

#endregion

namespace Libra.Input.SharpDX
{
    public sealed class SdxDirectInput : IDisposable
    {
        DIDirectInput diDirectInput;

        public SdxDirectInput() { }

        public SdxJoystick CreateJoystick()
        {
            if (diDirectInput == null)
                diDirectInput = new DIDirectInput();

            var devices = diDirectInput.GetDevices(DIDeviceType.Joystick, DIDeviceEnumerationFlags.AllDevices);

            if (devices.Count == 0)
            {
                devices = diDirectInput.GetDevices(DIDeviceType.Gamepad, DIDeviceEnumerationFlags.AllDevices);
            }

            var device = (devices.Count != 0) ? devices[0] : null;
            return new SdxJoystick(diDirectInput, device);
        }

        #region IDisposable

        bool disposed;

        ~SdxDirectInput()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                if (diDirectInput != null)
                    diDirectInput.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
