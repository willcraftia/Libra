#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class LinearDepthMapColorFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.LinearDepthMapColorFilterPS);
            }
        }

        #endregion

        #region ParametersPerCamera

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        struct ParametersPerCamera
        {
            [FieldOffset(0)]
            public float NearClipDistance;

            [FieldOffset(4)]
            public float FarClipDistance;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerCamera = (1 << 0)
        }

        #endregion

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerCamera;

        ParametersPerCamera parametersPerCamera;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public float NearClipDistance
        {
            get { return parametersPerCamera.NearClipDistance; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerCamera.NearClipDistance = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerCamera;
            }
        }

        public float FarClipDistance
        {
            get { return parametersPerCamera.FarClipDistance; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerCamera.FarClipDistance = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerCamera;
            }
        }

        public ShaderResourceView LinearDepthMap { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture
        {
            get { return null; }
            set { }
        }

        public SamplerState TextureSampler
        {
            get { return null; }
            set { }
        }

        public LinearDepthMapColorFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<LinearDepthMapColorFilter, SharedDeviceResource>();

            constantBufferPerCamera = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerCamera.Initialize<ParametersPerCamera>();

            parametersPerCamera.NearClipDistance = 1.0f;
            parametersPerCamera.FarClipDistance = 1000.0f;

            LinearDepthMapSampler = SamplerState.PointClamp;

            Enabled = true;

            dirtyFlags = DirtyFlags.ConstantBufferPerCamera;
        }

        public void Apply()
        {
            if ((dirtyFlags & DirtyFlags.ConstantBufferPerCamera) != 0)
            {
                constantBufferPerCamera.SetData(DeviceContext, parametersPerCamera);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerCamera;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerCamera;
            DeviceContext.PixelShaderResources[1] = LinearDepthMap;
            DeviceContext.PixelShaderSamplers[1] = LinearDepthMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~LinearDepthMapColorFilter()
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
