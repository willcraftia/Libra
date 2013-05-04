#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    /// <summary>
    /// ガウシアン ブラーを適用するシェーダです。
    /// </summary>
    /// <remarks>
    /// このシェーダは SpriteBatch と共に利用する事を前提としており、
    /// 頂点シェーダには SpriteBatch の頂点シェーダを用います。
    /// </remarks>
    public sealed class GaussianBlurEffect : IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.GaussianBlurPS);
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
            public float KernelSize;

            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxKernelSize)]
            public Kernel[] Kernels;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            KernelSize      = (1 << 0),
            KernelOffsets   = (1 << 1),
            KernelWeights   = (1 << 2),
            Constants       = (1 << 3)
        }

        #endregion

        public const int MaxRadius = 7;

        public const int MaxKernelSize = MaxRadius * 2 + 1;

        public const int DefaultRadius = 7;

        public const float DefaultAmount = 2.0f;

        Device device;

        SharedDeviceResource sharedDeviceResource;

        Constants constants;

        ConstantBuffer horizontalConstantBuffer;

        ConstantBuffer verticalConstantBufffer;

        int kernelSize;

        Kernel[] horizontalKernels;

        Kernel[] verticalKernels;

        int radius;

        float amount;

        int width;

        int height;

        DirtyFlags dirtyFlags;

        public GaussianBlurEffectPass Pass { get; set; }

        public int Radius
        {
            get { return radius; }
            set
            {
                if (value < 1 || MaxRadius < value) throw new ArgumentOutOfRangeException("value");

                if (radius == value) return;

                radius = value;

                dirtyFlags |= DirtyFlags.KernelSize | DirtyFlags.KernelWeights;
            }
        }

        public float Amount
        {
            get { return amount; }
            set
            {
                if (value < float.Epsilon) throw new ArgumentOutOfRangeException("value");

                if (amount == value) return;

                amount = value;

                dirtyFlags |= DirtyFlags.KernelWeights;
            }
        }

        public int Width
        {
            get { return width; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                if (width == value) return;

                width = value;

                dirtyFlags |= DirtyFlags.KernelOffsets;
            }
        }

        public int Height
        {
            get { return height; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                if (height == value) return;

                height = value;

                dirtyFlags |= DirtyFlags.KernelOffsets;
            }
        }

        public GaussianBlurEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<GaussianBlurEffect, SharedDeviceResource>();

            horizontalConstantBuffer = device.CreateConstantBuffer();
            horizontalConstantBuffer.Initialize<Constants>();

            verticalConstantBufffer = device.CreateConstantBuffer();
            verticalConstantBufffer.Initialize<Constants>();

            horizontalKernels = new Kernel[MaxKernelSize];
            verticalKernels = new Kernel[MaxKernelSize];

            radius = DefaultRadius;
            amount = DefaultAmount;
            width = 1;
            height = 1;

            dirtyFlags |= DirtyFlags.KernelSize | DirtyFlags.KernelOffsets | DirtyFlags.KernelWeights;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            SetKernelSize();
            SetKernelOffsets();
            SetKernelWeights();

            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constants.Kernels = horizontalKernels;
                horizontalConstantBuffer.SetData(context, constants);

                constants.Kernels = verticalKernels;
                verticalConstantBufffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            // 定数バッファの設定。
            switch (Pass)
            {
                case GaussianBlurEffectPass.Horizon:
                    context.PixelShaderConstantBuffers[0] = horizontalConstantBuffer;
                    break;
                case GaussianBlurEffectPass.Vertical:
                    context.PixelShaderConstantBuffers[0] = verticalConstantBufffer;
                    break;
                default:
                    throw new InvalidOperationException("Unknown direction: " + Pass);
            }

            // ピクセル シェーダの設定。
            context.PixelShader = sharedDeviceResource.PixelShader;
        }

        void SetKernelSize()
        {
            if ((dirtyFlags & DirtyFlags.KernelSize) != 0)
            {
                kernelSize = radius * 2 + 1;
                constants.KernelSize = kernelSize;

                dirtyFlags &= ~DirtyFlags.KernelSize;
                dirtyFlags |= DirtyFlags.KernelOffsets | DirtyFlags.KernelWeights | DirtyFlags.Constants;
            }
        }

        void SetKernelOffsets()
        {
            if ((dirtyFlags & DirtyFlags.KernelOffsets) != 0)
            {
                var dx = 1.0f / (float) width;
                var dy = 1.0f / (float) height;

                horizontalKernels[0].Offset = Vector2.Zero;
                verticalKernels[0].Offset = Vector2.Zero;

                for (int i = 0; i < kernelSize / 2; i++)
                {
                    int baseIndex = i * 2;
                    int left = baseIndex + 1;
                    int right = baseIndex + 2;

                    // XNA BloomPostprocess サンプルに従ってオフセットを決定。
                    float sampleOffset = i * 2 + 1.5f;
                    var offsetX = dx * sampleOffset;
                    var offsetY = dy * sampleOffset;

                    horizontalKernels[left].Offset.X = offsetX;
                    horizontalKernels[right].Offset.X = -offsetX;

                    verticalKernels[left].Offset.Y = offsetY;
                    verticalKernels[right].Offset.Y = -offsetY;
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
                var sigma = (float) radius / amount;

                var weight = MathHelper.CalculateGaussian(sigma, 0);

                horizontalKernels[0].Weight = weight;
                verticalKernels[0].Weight = weight;

                totalWeight += weight;

                for (int i = 0; i < kernelSize / 2; i++)
                {
                    int baseIndex = i * 2;
                    int left = baseIndex + 1;
                    int right = baseIndex + 2;

                    weight = MathHelper.CalculateGaussian(sigma, i + 1);
                    totalWeight += weight * 2;

                    horizontalKernels[left].Weight = weight;
                    horizontalKernels[right].Weight = weight;

                    verticalKernels[left].Weight = weight;
                    verticalKernels[right].Weight = weight;
                }

                // Normalize
                float inverseTotalWeights = 1.0f / totalWeight;
                for (int i = 0; i < kernelSize; i++)
                {
                    horizontalKernels[i].Weight *= inverseTotalWeights;
                    verticalKernels[i].Weight *= inverseTotalWeights;
                }

                dirtyFlags &= ~DirtyFlags.KernelWeights;
                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        #region IDisposable

        bool disposed;

        ~GaussianBlurEffect()
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
                horizontalConstantBuffer.Dispose();
                verticalConstantBufffer.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
