#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class RadialBlur : IPostprocess, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.RadialBlurPS);
            }
        }

        #endregion

        #region Kernel

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        struct Kernel
        {
            public Vector2 Offset;

            public float Weight;
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Explicit, Size = 16 + 16 * MaxKernelSize)]
        struct Constants
        {
            [FieldOffset(0)]
            public Vector2 Center;

            [FieldOffset(8)]
            public float Strength;

            [FieldOffset(12)]
            public int KernelSize;

            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxKernelSize)]
            public Kernel[] Kernels;
        }

        #endregion
        
        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            KernelOffsets   = (1 << 0),
            KernelWeights   = (1 << 1),
            Constants       = (1 << 2)
        }

        #endregion

        public const int MaxKernelSize = 32;

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

        float amount;

        int width;

        int height;

        DirtyFlags dirtyFlags;

        public Vector2 Center
        {
            get { return constants.Center; }
            set
            {
                constants.Center = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float Strength
        {
            get { return constants.Strength; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.Strength = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public int SampleCount
        {
            get { return constants.KernelSize; }
            set
            {
                if ((uint) MaxKernelSize < (uint) value) throw new ArgumentOutOfRangeException("value");

                constants.KernelSize = value;

                dirtyFlags |= DirtyFlags.KernelOffsets | DirtyFlags.KernelWeights | DirtyFlags.Constants;
            }
        }

        public float Amount
        {
            get { return amount; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                amount = value;

                dirtyFlags |= DirtyFlags.KernelWeights;
            }
        }

        public bool Enabled { get; set; }

        public RadialBlur(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<RadialBlur, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            amount = 1.0f;
            constants.Center = new Vector2(0.5f);
            constants.Strength = 10;
            constants.KernelSize = 10;
            constants.Kernels = new Kernel[MaxKernelSize];

            Enabled = true;

            dirtyFlags = DirtyFlags.KernelOffsets | DirtyFlags.KernelWeights | DirtyFlags.Constants;
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

                dirtyFlags |= DirtyFlags.KernelOffsets;
            }

            SetKernelOffsets();
            SetKernelWeights();

            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constantBuffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            context.PixelShaderConstantBuffers[0] = constantBuffer;
            context.PixelShader = sharedDeviceResource.PixelShader;
        }

        void SetKernelOffsets()
        {
            if ((dirtyFlags & DirtyFlags.KernelOffsets) != 0)
            {
                var dx = 1.0f / (float) width;
                var dy = 1.0f / (float) height;

                constants.Kernels[0].Offset = Vector2.Zero;

                for (int i = 1; i < constants.KernelSize; i++)
                {
                    float sampleOffset = i + 0.5f;
                    constants.Kernels[i].Offset.X = dx * sampleOffset;
                    constants.Kernels[i].Offset.Y = dy * sampleOffset;
                }

                dirtyFlags &= ~DirtyFlags.KernelOffsets;
                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        void SetKernelWeights()
        {
            if ((dirtyFlags & DirtyFlags.KernelWeights) != 0)
            {
                var totalWeight = 0.0f;
                var sigma = (float) constants.KernelSize / amount;

                for (int i = 0; i < constants.KernelSize; i++)
                {
                    var weight = MathHelper.CalculateGaussian(sigma, i);
                    
                    totalWeight += weight;
                    constants.Kernels[i].Weight = weight;
                }

                float invertTotalWeights = 1.0f / totalWeight;
                for (int i = 0; i < constants.KernelSize; i++)
                {
                    constants.Kernels[i].Weight *= invertTotalWeights;
                }

                dirtyFlags &= ~DirtyFlags.KernelWeights;
                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        #region IDisposable

        bool disposed;

        ~RadialBlur()
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
