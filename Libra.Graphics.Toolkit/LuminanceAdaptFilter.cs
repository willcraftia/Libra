#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class LuminanceAdaptFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.LuminanceAdaptFilterPS);
            }
        }

        #endregion

        #region ParametersPerFrame

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct ParametersPerFrame
        {
            public float DeltaTime;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerFrame = (1 << 0)
        }

        #endregion

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerFrame;

        ParametersPerFrame parametersPerFrame;

        DirtyFlags dirtyFlags = DirtyFlags.ConstantBufferPerFrame;

        public DeviceContext DeviceContext { get; private set; }

        public bool Enabled { get; set; }

        public float DeltaTime
        {
            get { return parametersPerFrame.DeltaTime; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerFrame.DeltaTime = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerFrame;
            }
        }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public ShaderResourceView LastTexture { get; set; }

        public SamplerState LastTextureSampler { get; set; }

        public LuminanceAdaptFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<LuminanceAdaptFilter, SharedDeviceResource>();

            constantBufferPerFrame = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerFrame.Initialize<ParametersPerFrame>();

            TextureSampler = SamplerState.PointClamp;
            LastTextureSampler = SamplerState.PointClamp;

            Enabled = true;
        }

        public void Apply()
        {
            if ((dirtyFlags & DirtyFlags.ConstantBufferPerFrame) != 0)
            {
                DeviceContext.SetData(constantBufferPerFrame, parametersPerFrame);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerFrame;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerFrame;
            DeviceContext.PixelShaderResources[0] = Texture;
            DeviceContext.PixelShaderResources[1] = LastTexture;
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
            DeviceContext.PixelShaderSamplers[1] = LastTextureSampler;
        }

        #region IDisposable

        bool disposed;

        ~LuminanceAdaptFilter()
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

                if (constantBufferPerFrame != null)
                    constantBufferPerFrame.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
