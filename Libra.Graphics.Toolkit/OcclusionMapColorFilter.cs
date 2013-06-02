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

        Device device;

        SharedDeviceResource sharedDeviceResource;

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

        public OcclusionMapColorFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<OcclusionMapColorFilter, SharedDeviceResource>();

            OcclusionMapSampler = SamplerState.PointClamp;

            Enabled = true;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[1] = OcclusionMap;
            context.PixelShaderSamplers[1] = OcclusionMapSampler;
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
