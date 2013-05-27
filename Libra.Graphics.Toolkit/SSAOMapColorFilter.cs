#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class SSAOMapColorFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.SSAOMapColorFilterPS);
            }
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        public ShaderResourceView SSAOMap { get; set; }

        public SamplerState SSAOMapSampler { get; set; }

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

        public SSAOMapColorFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<SSAOMapColorFilter, SharedDeviceResource>();

            SSAOMapSampler = SamplerState.PointClamp;

            Enabled = true;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[1] = SSAOMap;
            context.PixelShaderSamplers[1] = SSAOMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~SSAOMapColorFilter()
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
