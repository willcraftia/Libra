#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class LightScatteringFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.LightScatteringFilterPS);
            }
        }

        #endregion

        #region ParametersPerObject

        [StructLayout(LayoutKind.Explicit)]
        public struct ParametersPerObject
        {
            [FieldOffset(0)]
            public int SampleCount;

            [FieldOffset(16)]
            public float Density;

            [FieldOffset(20)]
            public float Decay;

            [FieldOffset(24)]
            public float Weight;

            [FieldOffset(28)]
            public float Exposure;
        }

        #endregion

        #region ParametersPerFrame

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct ParametersPerFrame
        {
            public Vector2 ScreenLightPosition;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObject = (1 << 0),
            ConstantBufferPerFrame  = (1 << 1)
        }

        #endregion

        public const int MaxSampleCount = 128;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerObject;

        ConstantBuffer constantBufferPerFrame;

        ParametersPerObject parametersPerObject;

        ParametersPerFrame parametersPerFrame;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public int SampleCount
        {
            get { return parametersPerObject.SampleCount; }
            set
            {
                if (value < 0 || MaxSampleCount < value) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.SampleCount = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float Density
        {
            get { return parametersPerObject.Density; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.Density = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float Decay
        {
            get { return parametersPerObject.Decay; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.Decay = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float Weight
        {
            get { return parametersPerObject.Weight; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.Weight = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float Exposure
        {
            get { return parametersPerObject.Exposure; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.Exposure = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public Vector2 ScreenLightPosition
        {
            get { return parametersPerFrame.ScreenLightPosition; }
            set
            {
                parametersPerFrame.ScreenLightPosition = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerFrame;
            }
        }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public LightScatteringFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<LightScatteringFilter, SharedDeviceResource>();

            constantBufferPerObject = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObject.Initialize<ParametersPerObject>();

            constantBufferPerFrame = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerFrame.Initialize<ParametersPerFrame>();

            parametersPerObject.SampleCount = 100;
            parametersPerObject.Density = 1.0f;
            parametersPerObject.Decay = 0.9f;
            parametersPerObject.Weight = 0.5f;
            parametersPerObject.Exposure = 1.0f;

            parametersPerFrame.ScreenLightPosition = Vector2.Zero;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerObject |
                DirtyFlags.ConstantBufferPerFrame;
        }

        public void Apply()
        {
            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObject) != 0)
            {
                constantBufferPerObject.SetData(DeviceContext, parametersPerObject);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObject;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerFrame) != 0)
            {
                constantBufferPerFrame.SetData(DeviceContext, parametersPerFrame);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerFrame;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerObject;
            DeviceContext.PixelShaderConstantBuffers[1] = constantBufferPerFrame;
            DeviceContext.PixelShaderResources[0] = Texture;
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
        }

        #region IDisposable

        bool disposed;

        ~LightScatteringFilter()
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
                constantBufferPerFrame.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
