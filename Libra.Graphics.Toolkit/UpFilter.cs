#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class UpFilter : IFilterEffect, IFilterEffectScale, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.UpFilterPS);
            }
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        float widthScale;

        float heightScale;

        public bool Enabled { get; set; }

        public float WidthScale
        {
            get { return widthScale; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                widthScale = value;
            }
        }

        public float HeightScale
        {
            get { return heightScale; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                heightScale = value;
            }
        }

        public UpFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<UpFilter, SharedDeviceResource>();

            widthScale = 4.0f;
            heightScale = 4.0f;

            Enabled = true;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            context.PixelShader = sharedDeviceResource.PixelShader;
        }

        #region IDisposable

        bool disposed;

        ~UpFilter()
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
