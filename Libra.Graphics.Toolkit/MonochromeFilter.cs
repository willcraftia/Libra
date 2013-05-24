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

        #region ParametersPerShader

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct ParametersPerShader
        {
            public float Cb;

            public float Cr;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerShader = (1 << 0)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerShader;

        ParametersPerShader parametersPerShader;

        DirtyFlags dirtyFlags;

        public float Cb
        {
            get { return parametersPerShader.Cb; }
            set
            {
                if (value < -1.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.Cb = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerShader;
            }
        }

        public float Cr
        {
            get { return parametersPerShader.Cr; }
            set
            {
                if (value < -1.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.Cr = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerShader;
            }
        }

        public bool Enabled { get; set; }

        public MonochromeFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<MonochromeFilter, SharedDeviceResource>();

            constantBufferPerShader = device.CreateConstantBuffer();
            constantBufferPerShader.Initialize<ParametersPerShader>();

            // グレー スケール。
            parametersPerShader.Cb = 0.0f;
            parametersPerShader.Cr = 0.0f;

            Enabled = true;

            dirtyFlags = DirtyFlags.ConstantBufferPerShader;
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

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerShader) != 0)
            {
                constantBufferPerShader.SetData(context, parametersPerShader);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerShader;
            }

            context.PixelShaderConstantBuffers[0] = constantBufferPerShader;
            context.PixelShader = sharedDeviceResource.PixelShader;
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
                constantBufferPerShader.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
