#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class ScanlineFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.ScanlineFilterPS);
            }
        }

        #endregion

        #region ParametersPerShader

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct ParametersPerShader
        {
            public float Density;

            public float Brightness;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerShader = (1 << 0)
        }

        #endregion

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerShader;

        ParametersPerShader parametersPerShader;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public float Density
        {
            get { return parametersPerShader.Density; }
            set
            {
                parametersPerShader.Density = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerShader;
            }
        }

        public float Brightness
        {
            get { return parametersPerShader.Brightness; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.Brightness = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerShader;
            }
        }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public ScanlineFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<ScanlineFilter, SharedDeviceResource>();

            constantBufferPerShader = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerShader.Initialize<ParametersPerShader>();

            parametersPerShader.Density = MathHelper.PiOver2;
            parametersPerShader.Brightness = 0.75f;

            Enabled = true;

            dirtyFlags = DirtyFlags.ConstantBufferPerShader;
        }

        public void Apply()
        {
            if ((dirtyFlags & DirtyFlags.ConstantBufferPerShader) != 0)
            {
                DeviceContext.SetData(constantBufferPerShader, parametersPerShader);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerShader;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerShader;
            DeviceContext.PixelShaderResources[0] = Texture;
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
        }

        #region IDisposable

        bool disposed;

        ~ScanlineFilter()
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
            }

            disposed = true;
        }

        #endregion
    }
}
