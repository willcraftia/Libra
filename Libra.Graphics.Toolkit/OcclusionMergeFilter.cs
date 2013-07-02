#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class OcclusionMergeFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.OcclusionMergeFilterPS);
            }
        }

        #endregion

        SharedDeviceResource sharedDeviceResource;

        public DeviceContext DeviceContext { get; private set; }

        public ShaderResourceView OtherOcclusionMap { get; set; }

        public SamplerState OtherOcclusionMapSampler { get; set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public OcclusionMergeFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<OcclusionMergeFilter, SharedDeviceResource>();

            TextureSampler = SamplerState.PointClamp;
            OtherOcclusionMapSampler = SamplerState.PointClamp;

            Enabled = true;
        }

        public void Apply()
        {
            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderResources[0] = Texture;
            DeviceContext.PixelShaderResources[1] = OtherOcclusionMap;
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
            DeviceContext.PixelShaderSamplers[1] = OtherOcclusionMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~OcclusionMergeFilter()
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
