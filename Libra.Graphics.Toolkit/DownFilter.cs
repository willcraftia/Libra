#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class DownFilter : IPostprocessPass, IPostprocessPassScale, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.DownFilterPS);
            }
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Sequential, Size = 16 * KernelSize)]
        public struct Constants
        {
            // X = offset X
            // Y = offset Y
            // Z = dummy
            // W = dummy
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = KernelSize)]
            public Vector4[] Kernels;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            Kernels     = (1 << 0),
            Constants   = (1 << 1)
        }

        #endregion

        const int KernelSize = 16;

        static readonly Vector2[] Offsets =
        {
            new Vector2( 1.5f, -1.5f),
            new Vector2( 1.5f, -0.5f),
            new Vector2( 1.5f,  0.5f),
            new Vector2( 1.5f,  1.5f),

            new Vector2( 0.5f, -1.5f),
            new Vector2( 0.5f, -0.5f),
            new Vector2( 0.5f,  0.5f),
            new Vector2( 0.5f,  1.5f),

            new Vector2(-0.5f, -1.5f),
            new Vector2(-0.5f, -0.5f),
            new Vector2(-0.5f,  0.5f),
            new Vector2(-0.5f,  1.5f),

            new Vector2(-1.5f, -1.5f),
            new Vector2(-1.5f, -0.5f),
            new Vector2(-1.5f,  0.5f),
            new Vector2(-1.5f,  1.5f),
        };

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

        int width;

        int height;

        DirtyFlags dirtyFlags;

        float widthScale;

        float heightScale;

        public bool Enabled { get; set; }

        public float WidthScale
        {
            get { return widthScale; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                widthScale = value;
            }
        }

        public float HeightScale
        {
            get { return heightScale; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                heightScale = value;
            }
        }

        public DownFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<DownFilter, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            constants.Kernels = new Vector4[KernelSize];

            widthScale = 0.25f;
            heightScale = 0.25f;

            Enabled = true;

            dirtyFlags = DirtyFlags.Kernels | DirtyFlags.Constants;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var viewport = context.Viewport;
            int currentWidth = (int) viewport.Width;
            int currentHeight = (int) viewport.Height;

            if (currentWidth != width || currentHeight != height)
            {
                width = currentWidth;
                height = currentHeight;

                dirtyFlags |= DirtyFlags.Kernels;
            }

            if ((dirtyFlags & DirtyFlags.Kernels) != 0)
            {
                for (int i = 0; i < KernelSize; i++)
                {
                    constants.Kernels[i].X = Offsets[i].X / width;
                    constants.Kernels[i].Y = Offsets[i].Y / width;

                    dirtyFlags &= ~DirtyFlags.Kernels;
                    dirtyFlags |= DirtyFlags.Constants;
                }
            }

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

        ~DownFilter()
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
