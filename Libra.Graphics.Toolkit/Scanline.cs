#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class Scanline : IPostprocess, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.ScanlinePS);
            }
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct Constants
        {
            public float Density;

            public float Brightness;
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

        public float Density
        {
            get { return constants.Density; }
            set
            {
                constants.Density = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float Brightness
        {
            get { return constants.Brightness; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                constants.Brightness = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public bool Enabled { get; set; }

        public Scanline(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<Scanline, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            constants.Density = MathHelper.PiOver2;
            constants.Brightness = 0.75f;

            Enabled = true;

            dirtyFlags = DirtyFlags.Constants;
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

        ~Scanline()
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
