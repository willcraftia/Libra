#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class BloomCombineFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.BloomCombineFilterPS);
            }
        }

        #endregion

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerObject;

        float baseIntensity;

        float baseSaturation;

        float bloomIntensity;

        float bloomSaturation;

        bool constantBufferPerObjectDirty;

        public DeviceContext DeviceContext { get; private set; }

        public float BaseIntensity
        {
            get { return baseIntensity; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value");

                if (baseIntensity == value) return;

                baseIntensity = value;

                constantBufferPerObjectDirty = true;
            }
        }

        public float BaseSaturation
        {
            get { return baseSaturation; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value");

                if (baseSaturation == value) return;

                baseSaturation = value;

                constantBufferPerObjectDirty = true;
            }
        }

        public float BloomIntensity
        {
            get { return bloomIntensity; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value");

                if (bloomIntensity == value) return;

                bloomIntensity = value;

                constantBufferPerObjectDirty = true;
            }
        }

        public float BloomSaturation
        {
            get { return bloomSaturation; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value");

                if (bloomSaturation == value) return;

                bloomSaturation = value;

                constantBufferPerObjectDirty = true;
            }
        }

        public ShaderResourceView BaseTexture { get; set; }

        public SamplerState BaseTextureSampler { get; set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public BloomCombineFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<BloomCombineFilter, SharedDeviceResource>();

            constantBufferPerObject = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObject.Initialize(16);

            baseIntensity = 1.0f;
            baseSaturation = 1.0f;
            bloomIntensity = 1.0f;
            bloomSaturation = 1.0f;

            constantBufferPerObjectDirty = true;

            Enabled = true;
        }

        public void Apply()
        {
            if (constantBufferPerObjectDirty)
            {
                var data = new Vector4(baseIntensity, baseSaturation, bloomIntensity, bloomSaturation);
                DeviceContext.SetData(constantBufferPerObject, data);

                constantBufferPerObjectDirty = false;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerObject;
            DeviceContext.PixelShaderResources[0] = Texture;
            DeviceContext.PixelShaderResources[1] = BaseTexture;
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
            DeviceContext.PixelShaderSamplers[1] = BaseTextureSampler;
        }

        #region IDisposable

        bool disposed;

        ~BloomCombineFilter()
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
                constantBufferPerObject.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
