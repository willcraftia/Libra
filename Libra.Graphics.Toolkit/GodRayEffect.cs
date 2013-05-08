#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class GodRayEffect : IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.GodRayPS);
            }
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Explicit)]
        public struct Constants
        {
            [FieldOffset(0)]
            public int SampleCount;

            [FieldOffset(16)]
            public Vector2 ScreenLightPosition;

            [FieldOffset(32)]
            public float Density;

            [FieldOffset(36)]
            public float Decay;

            [FieldOffset(40)]
            public float Weight;

            [FieldOffset(44)]
            public float Exposure;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            Constants = (1 << 2)
        }

        #endregion

        public const int MaxSampleCount = 32;

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

        DirtyFlags dirtyFlags;

        public int SampleCount
        {
            get { return constants.SampleCount; }
            set
            {
                if (value < 0 || MaxSampleCount < value) throw new ArgumentOutOfRangeException("value");

                constants.SampleCount = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public Vector2 ScreenLightPosition
        {
            get { return constants.ScreenLightPosition; }
            set
            {
                constants.ScreenLightPosition = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float Density
        {
            get { return constants.Density; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.Density = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float Decay
        {
            get { return constants.Decay; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.Decay = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float Weight
        {
            get { return constants.Weight; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.Weight = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float Exposure
        {
            get { return constants.Exposure; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.Exposure = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public ShaderResourceView SceneMap { get; set; }

        public ShaderResourceView OcclusionMap { get; set; }

        public GodRayEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<GodRayEffect, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            constants.SampleCount = 32;
            constants.ScreenLightPosition = Vector2.Zero;
            //constants.Density = 1.0f;
            constants.Density = 0.1f;
            constants.Decay = 0.8f;
            constants.Weight = 0.8f;
            constants.Exposure = 0.8f;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constantBuffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            context.PixelShaderConstantBuffers[0] = constantBuffer;
            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[0] = SceneMap;
            context.PixelShaderResources[1] = OcclusionMap;

            context.PixelShaderSamplers[0] = SamplerState.LinearClamp;
            context.PixelShaderSamplers[1] = SamplerState.LinearClamp;
        }

        #region IDisposable

        bool disposed;

        ~GodRayEffect()
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
