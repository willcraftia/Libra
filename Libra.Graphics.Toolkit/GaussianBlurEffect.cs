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
        #region DeviceResources

        sealed class DeviceResources
        {
            Device device;

            public PixelShader PixelShader { get; private set; }

            internal DeviceResources(Device device)
            {
                this.device = device;

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

        public const int MaxRadius = 15;

        public const int MaxKernelSize = MaxRadius * 2 + 1;

        public const float MinAmount = 0.001f;

        public const int DefaultRadius = 7;

        public const float DefaultAmount = 2.0f;

        static readonly SharedResourcePool<Device, DeviceResources> DeviceResourcesPool;

        Device device;

        DeviceResources deviceResources;

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

                dirtyFlags |= DirtyFlags.KernelSize;
            }
        }

        public float Amount
        {
            get { return amount; }
            set
            {
                if (value < MinAmount) throw new ArgumentOutOfRangeException("value");

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

        static GaussianBlurEffect()
        {
            DeviceResourcesPool = new SharedResourcePool<Device, DeviceResources>(
                (device) => { return new DeviceResources(device); });
        }

        public GaussianBlurEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            deviceResources = DeviceResourcesPool.Get(device);

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
            context.PixelShader = deviceResources.PixelShader;
        }

        void SetKernelSize()
        {
            if ((dirtyFlags & DirtyFlags.KernelSize) != 0)
            {
                kernelSize = radius * 2 + 1;
                constants.KernelSize = kernelSize;

                dirtyFlags &= ~DirtyFlags.KernelSize;
                dirtyFlags |= DirtyFlags.Constants;
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

                    var offsetX = i * dx;
                    var offsetY = i * dy;

                    horizontalKernels[left].Offset.X = -offsetX;
                    horizontalKernels[right].Offset.X = offsetX;

                    verticalKernels[left].Offset.Y = -offsetY;
                    verticalKernels[right].Offset.Y = offsetY;
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

                horizontalKernels[0].Weight = MathHelper.CalculateGaussian(sigma, 0);

                for (int i = 0; i < kernelSize / 2; i++)
                {
                    int baseIndex = i * 2;
                    int left = baseIndex + 1;
                    int right = baseIndex + 2;

                    var weight = MathHelper.CalculateGaussian(sigma, i + 1);
                    totalWeight += weight * 2;

                    horizontalKernels[left].Weight = weight;
                    horizontalKernels[right].Weight = weight;

                    verticalKernels[left].Weight = weight;
                    verticalKernels[right].Weight = weight;
                }

                // Normalize
                for (int i = 0; i < kernelSize; i++)
                {
                    horizontalKernels[i].Weight /= totalWeight;
                    verticalKernels[i].Weight /= totalWeight;
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
                deviceResources = null;
                horizontalConstantBuffer.Dispose();
                verticalConstantBufffer.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
