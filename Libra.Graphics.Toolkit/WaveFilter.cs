#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class WaveFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.WaveFilterPS);
            }
        }

        #endregion

        #region ConstantsPerShader

        [StructLayout(LayoutKind.Explicit, Size = 16 + 16 * KernelSize)]
        public struct ConstantsPerShader
        {
            [FieldOffset(0)]
            public float Stiffness;

            // XY: テクセル オフセット
            // ZW: 整列用ダミー
            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = KernelSize)]
            public Vector4[] Kernel;
        }

        #endregion

        #region ConstantsPerFrame

        public struct ConstantsPerFrame
        {
            public Vector2 NewWavePosition;

            public float NewWaveRadius;

            public float NewWaveVeclocity;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            Kernel              = (1 << 0),
            ConstantsPerShader  = (1 << 1),
            ConstantsPerFrame   = (1 << 2)
        }

        #endregion

        const int KernelSize = 4;

        static readonly Vector2[] Offsets =
        {
            new Vector2( 1.5f,  0.0f),
            new Vector2(-1.5f,  0.0f),
            new Vector2( 0.0f,  1.5f),
            new Vector2( 0.0f, -1.5f),
        };

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerShader;

        ConstantBuffer constantBufferPerFrame;

        ConstantsPerShader constantsPerShader;

        ConstantsPerFrame constantsPerFrame;

        int width;

        int height;

        DirtyFlags dirtyFlags;

        public float Stiffness
        {
            get { return constantsPerShader.Stiffness; }
            set
            {
                constantsPerShader.Stiffness = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        public bool Enabled { get; set; }

        public WaveFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<WaveFilter, SharedDeviceResource>();

            constantBufferPerShader = device.CreateConstantBuffer();
            constantBufferPerShader.Initialize<ConstantsPerShader>();

            constantBufferPerFrame = device.CreateConstantBuffer();
            constantBufferPerFrame.Initialize<ConstantsPerFrame>();

            constantsPerShader.Stiffness = 0.0f;
            constantsPerShader.Kernel = new Vector4[KernelSize];

            constantsPerFrame.NewWavePosition = Vector2.Zero;
            constantsPerFrame.NewWaveRadius = 0.0f;
            constantsPerFrame.NewWaveVeclocity = 0.0f;

            Enabled = true;

            dirtyFlags = DirtyFlags.Kernel | DirtyFlags.ConstantsPerShader | DirtyFlags.ConstantsPerFrame;
        }

        public void AddWave(Vector2 position, float radius, float velocity)
        {
            if (position.X < 0.0f || 1.0f < position.X ||
                position.Y < 0.0f || 1.0f < position.Y)
                throw new ArgumentOutOfRangeException("position");
            if (radius <= 0.0f) throw new ArgumentOutOfRangeException("radius");
            if (velocity <= 0.0f) throw new ArgumentOutOfRangeException("velocity");

            constantsPerFrame.NewWavePosition = position;
            constantsPerFrame.NewWaveRadius = radius;
            constantsPerFrame.NewWaveVeclocity = velocity;

            dirtyFlags |= DirtyFlags.ConstantsPerFrame;
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
                    constantsPerShader.Kernel[i].X = Offsets[i].X / (float) width;
                    constantsPerShader.Kernel[i].Y = Offsets[i].Y / (float) height;

                    dirtyFlags &= ~DirtyFlags.Kernel;
                    dirtyFlags |= DirtyFlags.ConstantsPerShader;
                }
            }

            if ((dirtyFlags & DirtyFlags.ConstantsPerShader) != 0)
            {
                constantBufferPerShader.SetData(context, constantsPerShader);

                dirtyFlags &= ~DirtyFlags.ConstantsPerShader;
            }

            if ((dirtyFlags & DirtyFlags.ConstantsPerFrame) != 0)
            {
                constantBufferPerFrame.SetData(context, constantsPerFrame);

                dirtyFlags &= ~DirtyFlags.ConstantsPerFrame;
            }

            context.PixelShaderConstantBuffers[0] = constantBufferPerShader;
            context.PixelShaderConstantBuffers[1] = constantBufferPerFrame;
            context.PixelShader = sharedDeviceResource.PixelShader;
        }

        #region IDisposable

        bool disposed;

        ~WaveFilter()
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
