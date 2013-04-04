#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class BloomEffect : IEffect
    {
        #region DeviceResources

        sealed class DeviceResources
        {
            Device device;

            public PixelShader ExtractPixelShader { get; private set; }

            public PixelShader CombinePixelShader { get; private set; }

            internal DeviceResources(Device device)
            {
                this.device = device;

                ExtractPixelShader = device.CreatePixelShader();
                ExtractPixelShader.Initialize(Resources.BloomExtractPS);

                CombinePixelShader = device.CreatePixelShader();
                CombinePixelShader.Initialize(Resources.BloomCombinePS);
            }
        }

        #endregion

        static readonly SharedResourcePool<Device, DeviceResources> DeviceResourcesPool;

        Device device;

        DeviceResources deviceResources;

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

        static BloomEffect()
        {
            DeviceResourcesPool = new SharedResourcePool<Device, DeviceResources>(
                (device) => { return new DeviceResources(device); });
        }

        public BloomEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            deviceResources = DeviceResourcesPool.Get(device);

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
            context.PixelShader = deviceResources.ExtractPixelShader;
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
            context.PixelShader = deviceResources.CombinePixelShader;

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
