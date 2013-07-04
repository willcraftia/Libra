#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class MonochromeFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.MonochromeFilterPS);
            }
        }

        #endregion

        #region ParametersPerObject

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct ParametersPerObject
        {
            public float Cb;

            public float Cr;
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

        ParametersPerObject parametersPerObject;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public float Cb
        {
            get { return parametersPerObject.Cb; }
            set
            {
                if (value < -1.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.Cb = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float Cr
        {
            get { return parametersPerObject.Cr; }
            set
            {
                if (value < -1.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.Cr = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public MonochromeFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<MonochromeFilter, SharedDeviceResource>();

            constantBufferPerObject = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObject.Initialize<ParametersPerObject>();

            // グレー スケール。
            parametersPerObject.Cb = 0.0f;
            parametersPerObject.Cr = 0.0f;

            Enabled = true;

            dirtyFlags = DirtyFlags.ConstantBufferPerObject;
        }

        public void SetupGrayscale()
        {
            Cb = 0.0f;
            Cr = 0.0f;
        }

        public void SetupSepiaTone()
        {
            Cb = -0.1f;
            Cr = 0.1f;
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
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
        }

        #region IDisposable

        bool disposed;

        ~MonochromeFilter()
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
            }

            disposed = true;
        }

        #endregion
    }
}
