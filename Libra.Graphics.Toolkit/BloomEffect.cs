#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class BloomEffect : IEffect
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource : SharedDeviceResourceBase
        {
            public PixelShader ExtractPixelShader { get; private set; }

            public PixelShader CombinePixelShader { get; private set; }

            public SharedDeviceResource(Device device)
                : base(device)
            {
                ExtractPixelShader = Device.CreatePixelShader();
                ExtractPixelShader.Initialize(Resources.BloomExtractPS);

                CombinePixelShader = Device.CreatePixelShader();
                CombinePixelShader.Initialize(Resources.BloomCombinePS);
            }
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer extractConstantBuffer;

        ConstantBuffer combineConstantBuffer;

        bool extractConstantsDirty;

        float bloomThreshold;

        float bloomIntensity;

        float baseIntensity;

        float bloomSaturation;

        float baseSaturation;

        bool combineConstantsDirty;

        public float BloomThreshold
        {
            get { return bloomThreshold; }
            set
            {
                if (bloomThreshold == value) return;

                bloomThreshold = value;

                extractConstantsDirty = true;
            }
        }

        public float BloomIntensity
        {
            get { return bloomIntensity; }
            set
            {
                if (bloomIntensity == value) return;

                bloomIntensity = value;

                combineConstantsDirty = true;
            }
        }

        public float BaseIntensity
        {
            get { return baseIntensity; }
            set
            {
                if (baseIntensity == value) return;

                baseIntensity = value;

                combineConstantsDirty = true;
            }
        }

        public float BloomSaturation
        {
            get { return bloomSaturation; }
            set
            {
                if (bloomSaturation == value) return;

                bloomSaturation = value;

                combineConstantsDirty = true;
            }
        }

        public float BaseSaturation
        {
            get { return baseSaturation; }
            set
            {
                if (baseSaturation == value) return;

                baseSaturation = value;

                combineConstantsDirty = true;
            }
        }

        public ShaderResourceView BloomTexture { get; set; }

        public ShaderResourceView BaseTexture { get; set; }

        public BloomEffectPass Pass { get; set; }

        public BloomEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<BloomEffect, SharedDeviceResource>();

            extractConstantBuffer = device.CreateConstantBuffer();
            extractConstantBuffer.Initialize(16);

            combineConstantBuffer = device.CreateConstantBuffer();
            combineConstantBuffer.Initialize(16);

            Pass = BloomEffectPass.Extract;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            switch (Pass)
            {
                case BloomEffectPass.Extract:
                    ApplyExtractPass(context);
                    break;
                case BloomEffectPass.Combine:
                    ApplyCombinePass(context);
                    break;
                default:
                    throw new InvalidOperationException("Unknown pass: " + Pass);
            }
        }

        void ApplyExtractPass(DeviceContext context)
        {
            if (extractConstantsDirty)
            {
                extractConstantBuffer.SetData(context, bloomThreshold);

                extractConstantsDirty = false;
            }

            context.PixelShaderConstantBuffers[0] = extractConstantBuffer;
            context.PixelShader = sharedDeviceResource.ExtractPixelShader;
        }

        void ApplyCombinePass(DeviceContext context)
        {
            if (combineConstantsDirty)
            {
                var data = new Vector4(bloomIntensity, baseIntensity, bloomSaturation, baseSaturation);
                combineConstantBuffer.SetData(context, data);

                combineConstantsDirty = false;
            }

            context.PixelShaderConstantBuffers[0] = combineConstantBuffer;
            context.PixelShader = sharedDeviceResource.CombinePixelShader;

            context.PixelShaderResources[0] = BloomTexture;
            context.PixelShaderResources[1] = BaseTexture;
        }

        #region IDisposable

        bool disposed;

        ~BloomEffect()
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
            }

            disposed = true;
        }

        #endregion
    }
}
