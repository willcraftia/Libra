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

        #region ParametersPerShader

        [StructLayout(LayoutKind.Explicit)]
        public struct ParametersPerShader
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
            ConstantBufferPerShader = (1 << 0),
            ConstantBufferPerFrame  = (1 << 1)
        }

        #endregion

        public const int MaxSampleCount = 128;

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerShader;

        ConstantBuffer constantBufferPerFrame;

        ParametersPerShader parametersPerShader;

        ParametersPerFrame parametersPerFrame;

        DirtyFlags dirtyFlags;

        public int SampleCount
        {
            get { return parametersPerShader.SampleCount; }
            set
            {
                if (value < 0 || MaxSampleCount < value) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.SampleCount = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerShader;
            }
        }

        public float Density
        {
            get { return parametersPerShader.Density; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.Density = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerShader;
            }
        }

        public float Decay
        {
            get { return parametersPerShader.Decay; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.Decay = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerShader;
            }
        }

        public float Weight
        {
            get { return parametersPerShader.Weight; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.Weight = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerShader;
            }
        }

        public float Exposure
        {
            get { return parametersPerShader.Exposure; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.Exposure = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerShader;
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

        public LightScatteringFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<LightScatteringFilter, SharedDeviceResource>();

            constantBufferPerShader = device.CreateConstantBuffer();
            constantBufferPerShader.Initialize<ParametersPerShader>();

            constantBufferPerFrame = device.CreateConstantBuffer();
            constantBufferPerFrame.Initialize<ParametersPerFrame>();

            parametersPerShader.SampleCount = 100;
            parametersPerShader.Density = 1.0f;
            parametersPerShader.Decay = 0.9f;
            parametersPerShader.Weight = 0.5f;
            parametersPerShader.Exposure = 1.0f;

            parametersPerFrame.ScreenLightPosition = Vector2.Zero;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerShader |
                DirtyFlags.ConstantBufferPerFrame;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.ConstantBufferPerShader) != 0)
            {
                constantBufferPerShader.SetData(context, parametersPerShader);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerShader;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerFrame) != 0)
            {
                constantBufferPerFrame.SetData(context, parametersPerFrame);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerFrame;
            }

            context.PixelShaderConstantBuffers[0] = constantBufferPerShader;
            context.PixelShaderConstantBuffers[1] = constantBufferPerFrame;
            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[0] = Texture;
            context.PixelShaderSamplers[0] = SamplerState.LinearClamp;
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
                constantBufferPerShader.Dispose();
                constantBufferPerFrame.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
