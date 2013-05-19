#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class SSAOMapVisualize : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.SSAOMapVisualizePS);
            }
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        public ShaderResourceView SSAOMap { get; set; }

        public SamplerState SSAOMapSampler { get; set; }

        public bool Enabled { get; set; }

        public SSAOMapVisualize(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<SSAOMapVisualize, SharedDeviceResource>();

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

        ~SSAOMapVisualize()
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
