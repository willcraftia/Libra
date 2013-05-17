#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class Monochrome : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.MonochromePS);
            }
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct Constants
        {
            public float Cb;

            public float Cr;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            Constants = (1 << 0)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

        DirtyFlags dirtyFlags;

        public float Cb
        {
            get { return constants.Cb; }
            set
            {
                if (value < -1.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                constants.Cb = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float Cr
        {
            get { return constants.Cr; }
            set
            {
                if (value < -1.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                constants.Cr = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public bool Enabled { get; set; }

        public Monochrome(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<Monochrome, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            // グレー スケール。
            constants.Cb = 0.0f;
            constants.Cr = 0.0f;

            Enabled = true;

            dirtyFlags = DirtyFlags.Constants;
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

            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constantBuffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            context.PixelShaderConstantBuffers[0] = constantBuffer;
            context.PixelShader = sharedDeviceResource.PixelShader;
        }

        #region IDisposable

        bool disposed;

        ~Monochrome()
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
