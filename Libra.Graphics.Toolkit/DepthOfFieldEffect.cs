#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class DepthOfFieldEffect : IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.DepthOfFieldPS);
            }
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Sequential)]
        public struct Constants
        {
            public float FocusRange;

            public float FocusDistance;
            
            public float NearClipDistance;
            
            public float FarClipDistance;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            Clipping    = (1 << 0),
            Constants   = (1 << 1)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

        float nearClipDistance;

        float farClipDistance;

        DirtyFlags dirtyFlags;

        public float FocusRange
        {
            get { return constants.FocusRange; }
            set
            {
                constants.FocusRange = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float FocusDistance
        {
            get { return constants.FocusDistance; }
            set
            {
                constants.FocusDistance = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float NearClipDistance
        {
            get { return nearClipDistance; }
            set
            {
                nearClipDistance = value;

                dirtyFlags |= DirtyFlags.Clipping | DirtyFlags.Constants;
            }
        }

        public float FarClipDistance
        {
            get { return farClipDistance; }
            set
            {
                farClipDistance = value;

                dirtyFlags |= DirtyFlags.Clipping | DirtyFlags.Constants;
            }
        }

        public ShaderResourceView NormalSceneMap { get; set; }

        public ShaderResourceView BluredSceneMap { get; set; }

        public ShaderResourceView DepthMap { get; set; }

        public DepthOfFieldEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<DepthOfFieldEffect, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            constants.FocusDistance = 10.0f;
            constants.FocusRange = 100.0f;
            nearClipDistance = 1.0f;
            farClipDistance = 1000.0f;

            dirtyFlags = DirtyFlags.Clipping | DirtyFlags.Constants;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.Clipping) != 0)
            {
                constants.FarClipDistance = farClipDistance / (farClipDistance - nearClipDistance);
                constants.NearClipDistance = nearClipDistance * constants.FarClipDistance;

                dirtyFlags &= ~DirtyFlags.Clipping;
                dirtyFlags |= DirtyFlags.Constants;
            }

            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constantBuffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            context.VertexShaderConstantBuffers[0] = constantBuffer;
            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[0] = NormalSceneMap;
            context.PixelShaderResources[1] = BluredSceneMap;
            context.PixelShaderResources[2] = DepthMap;

            context.PixelShaderSamplers[0] = SamplerState.LinearClamp;
            context.PixelShaderSamplers[1] = SamplerState.LinearClamp;
            context.PixelShaderSamplers[2] = SamplerState.LinearClamp;
        }

        #region IDisposable

        bool disposed;

        ~DepthOfFieldEffect()
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
