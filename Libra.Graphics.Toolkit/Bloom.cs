#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class Bloom : IPostprocessor, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.BloomPS);
            }
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        float baseIntensity;

        float baseSaturation;

        float bloomIntensity;

        float bloomSaturation;

        bool constantsDirty;

        public float BaseIntensity
        {
            get { return baseIntensity; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value");

                if (baseIntensity == value) return;

                baseIntensity = value;

                constantsDirty = true;
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

                constantsDirty = true;
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

                constantsDirty = true;
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

                constantsDirty = true;
            }
        }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public ShaderResourceView BloomTexture { get; set; }

        public Bloom(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<Bloom, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize(16);
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            if (constantsDirty)
            {
                var data = new Vector4(baseIntensity, baseSaturation, bloomIntensity, bloomSaturation);
                constantBuffer.SetData(context, data);

                constantsDirty = false;
            }

            context.PixelShaderConstantBuffers[0] = constantBuffer;
            context.PixelShaderResources[0] = Texture;
            context.PixelShaderResources[1] = BloomTexture;
            context.PixelShaderSamplers[0] = SamplerState.PointClamp;
            context.PixelShaderSamplers[1] = SamplerState.PointClamp;
            context.PixelShader = sharedDeviceResource.PixelShader;
        }

        #region IDisposable

        bool disposed;

        ~Bloom()
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
                constantBuffer.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
