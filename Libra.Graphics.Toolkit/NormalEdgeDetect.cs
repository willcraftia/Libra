#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class NormalEdgeDetect : IPostprocessPass, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.NormalEdgeDetectPS);
            }
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Sequential, Size = 16 * KernelSize)]
        public struct Constants
        {
            // XY: テクセル オフセット
            // ZW: 整列用ダミー
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = KernelSize)]
            public Vector4[] Kernel;
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

        const int KernelSize = 4;

        static readonly Vector2[] Offsets =
        {
            new Vector2( 0,  1),
            new Vector2( 1,  0),
            new Vector2( 0, -1),
            new Vector2(-1,  0),
        };

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

        int width;

        int height;

        DirtyFlags dirtyFlags;

        public bool Enabled { get; set; }

        public ShaderResourceView NormalMap { get; set; }

        public SamplerState NormalMapSampler { get; set; }

        public NormalEdgeDetect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<NormalEdgeDetect, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            constants.Kernel = new Vector4[KernelSize];

            NormalMapSampler = SamplerState.LinearClamp;

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
                    constants.Kernel[i].X = Offsets[i].X / width;
                    constants.Kernel[i].Y = Offsets[i].Y / width;

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

            context.PixelShaderResources[1] = NormalMap;
            context.PixelShaderSamplers[1] = NormalMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~NormalEdgeDetect()
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
