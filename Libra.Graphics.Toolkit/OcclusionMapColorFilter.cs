#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class OcclusionMapColorFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.OcclusionMapColorFilterPS);
            }
        }

        #endregion

        SharedDeviceResource sharedDeviceResource;

        public DeviceContext DeviceContext { get; private set; }

        public ShaderResourceView OcclusionMap { get; set; }

        public SamplerState OcclusionMapSampler { get; set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture
        {
            get { return null; }
            set { }
        }

        public SamplerState TextureSampler
        {
            get { return null; }
            set { }
        }

        public OcclusionMapColorFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<OcclusionMapColorFilter, SharedDeviceResource>();

            OcclusionMapSampler = SamplerState.PointClamp;

            Enabled = true;
        }

        public void Apply()
        {
            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderResources[1] = OcclusionMap;
            DeviceContext.PixelShaderSamplers[1] = OcclusionMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~OcclusionMapColorFilter()
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
