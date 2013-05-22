#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class HeightToGradientFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.HeightToGradientFilterPS);
            }
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Sequential, Size = 16 * KernelSize)]
        struct Constants
        {
            // XY: テクセル オフセット
            // ZW:  整列用ダミー
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = KernelSize)]
            public Vector4[] Kernel;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            Kernel      = (1 << 0),
            Constants   = (1 << 1)
        }

        #endregion

        public const int KernelSize = 4;

        static readonly Vector2[] Offsets =
        {
            new Vector2(-1.5f,  0.0f),
            new Vector2( 1.5f,  0.0f),
            new Vector2( 0.0f, -1.5f),
            new Vector2( 0.0f,  1.5f),
        };

        Device device;

        SharedDeviceResource sharedDeviceResource;

        Constants constants;

        ConstantBuffer constantBuffer;

        int width;

        int height;

        DirtyFlags dirtyFlags;

        public ShaderResourceView HeightMap { get; set; }

        public SamplerState HeightMapSampler { get; set; }

        public bool Enabled { get; set; }

        public HeightToGradientFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<HeightToGradientFilter, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();
            
            width = 1;
            height = 1;

            constants.Kernel = new Vector4[KernelSize];

            HeightMapSampler = SamplerState.LinearClamp;

            Enabled = true;

            dirtyFlags = DirtyFlags.Kernel | DirtyFlags.Constants;
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

                dirtyFlags |= DirtyFlags.Kernel;
            }

            if ((dirtyFlags & DirtyFlags.Kernel) != 0)
            {
                for (int i = 0; i < KernelSize; i++)
                {
                    constants.Kernel[i].X = Offsets[i].X / (float) width;
                    constants.Kernel[i].Y = Offsets[i].Y / (float) height;

                    dirtyFlags &= ~DirtyFlags.Kernel;
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

            context.PixelShaderResources[1] = HeightMap;
            context.PixelShaderSamplers[1] = HeightMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~HeightToGradientFilter()
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
