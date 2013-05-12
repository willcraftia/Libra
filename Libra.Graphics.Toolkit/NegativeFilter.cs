#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class NegativeFilter : IPostprocess, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.NegativeFilterPS);
            }
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        public bool Enabled { get; set; }

        public NegativeFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<NegativeFilter, SharedDeviceResource>();

            Enabled = true;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            context.PixelShader = sharedDeviceResource.PixelShader;
        }

        #region IDisposable

        bool disposed;

        ~NegativeFilter()
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
                sharedDeviceResource = null;
            }

            disposed = true;
        }

        #endregion
    }
}
