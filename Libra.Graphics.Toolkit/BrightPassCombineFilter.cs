#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class BrightPassCombineFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.BrightPassCombineFilterPS);
            }
        }

        #endregion

        #region ParametersPerObject

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct ParametersPerObject
        {
            [FieldOffset(0)]
            public float Threshold;

            [FieldOffset(4)]
            public float MiddleGrey;

            [FieldOffset(8)]
            public float MaxLuminance;

            [FieldOffset(12)]
            public float BloomIntensity;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObject = (1 << 0)
        }

        #endregion

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerObject;

        ParametersPerObject parametersPerObject = new ParametersPerObject
        {
            Threshold = 0.5f,
            MiddleGrey = 0.5f,
            MaxLuminance = 1.0f,
            BloomIntensity = 1.0f
        };

        DirtyFlags dirtyFlags = DirtyFlags.ConstantBufferPerObject;

        public DeviceContext DeviceContext { get; private set; }

        public bool Enabled { get; set; }

        public float Threshold
        {
            get { return parametersPerObject.Threshold; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.Threshold = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float MiddleGrey
        {
            get { return parametersPerObject.MiddleGrey; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.MiddleGrey = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float MaxLuminance
        {
            get { return parametersPerObject.MaxLuminance; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.MaxLuminance = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float BloomIntensity
        {
            get { return parametersPerObject.BloomIntensity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.BloomIntensity = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public ShaderResourceView LuminanceAverageMap { get; set; }

        public SamplerState LuminanceAverageMapSampler { get; set; }

        public ShaderResourceView SourceTexture { get; set; }

        public SamplerState SourceTextureSampler { get; set; }

        public BrightPassCombineFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<BrightPassCombineFilter, SharedDeviceResource>();

            constantBufferPerObject = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObject.Initialize<ParametersPerObject>();

            LuminanceAverageMapSampler = SamplerState.PointClamp;

            Enabled = true;
        }

        public void Apply()
        {
            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObject) != 0)
            {
                DeviceContext.SetData(constantBufferPerObject, parametersPerObject);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObject;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerObject;
            DeviceContext.PixelShaderResources[0] = Texture;
            DeviceContext.PixelShaderResources[1] = LuminanceAverageMap;
            DeviceContext.PixelShaderResources[2] = SourceTexture;
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
            DeviceContext.PixelShaderSamplers[1] = LuminanceAverageMapSampler;
            DeviceContext.PixelShaderSamplers[2] = SourceTextureSampler;
        }

        #region IDisposable

        bool disposed;

        ~BrightPassCombineFilter()
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

                if (constantBufferPerObject != null)
                    constantBufferPerObject.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
