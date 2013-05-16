#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    /// <summary>
    /// ガウシアン ブラーのピクセル シェーダを管理するクラスです。
    /// </summary>
    /// <remarks>
    /// このクラスは、SpriteBatch あるいは FullscreenQuad の頂点シェーダの利用を前提としています。
    /// </remarks>
    public sealed class GaussianBlurCore : IDisposable
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

        #region Constants

        [StructLayout(LayoutKind.Explicit, Size = 16 + 16 * MaxKernelSize)]
        struct Constants
        {
            [FieldOffset(0)]
            public float KernelSize;

            // XY: テクセル オフセット
            // Z:  重み
            // W:  整列用ダミー
            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxKernelSize)]
            public Vector4[] Kernel;
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

        Vector4[] horizontalKernel;

        Vector4[] verticalKernel;

        int radius;

        float amount;

        int width;

        int height;

        DirtyFlags dirtyFlags;

        public GaussianBlurPass Pass { get; set; }

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

        public GaussianBlurCore(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<GaussianBlurCore, SharedDeviceResource>();

            horizontalConstantBuffer = device.CreateConstantBuffer();
            horizontalConstantBuffer.Initialize<Constants>();

            verticalConstantBufffer = device.CreateConstantBuffer();
            verticalConstantBufffer.Initialize<Constants>();

            horizontalKernel = new Vector4[MaxKernelSize];
            verticalKernel = new Vector4[MaxKernelSize];

            radius = DefaultRadius;
            amount = DefaultAmount;
            width = 1;
            height = 1;

            dirtyFlags |= DirtyFlags.KernelSize | DirtyFlags.KernelOffsets | DirtyFlags.KernelWeights;
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

            SetKernelSize();
            SetKernelOffsets();
            SetKernelWeights();

            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constants.Kernel = horizontalKernel;
                horizontalConstantBuffer.SetData(context, constants);

                constants.Kernel = verticalKernel;
                verticalConstantBufffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            // 定数バッファの設定。
            switch (Pass)
            {
                case GaussianBlurPass.Horizon:
                    context.PixelShaderConstantBuffers[0] = horizontalConstantBuffer;
                    break;
                case GaussianBlurPass.Vertical:
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

                horizontalKernel[0].X = 0.0f;
                horizontalKernel[0].Y = 0.0f;
                verticalKernel[0].X = 0.0f;
                verticalKernel[0].Y = 0.0f;

                for (int i = 0; i < kernelSize / 2; i++)
                {
                    int baseIndex = i * 2;
                    int left = baseIndex + 1;
                    int right = baseIndex + 2;

                    // XNA BloomPostprocess サンプルに従ってオフセットを決定。
                    float sampleOffset = i * 2 + 1.5f;
                    var offsetX = dx * sampleOffset;
                    var offsetY = dy * sampleOffset;

                    horizontalKernel[left].X = offsetX;
                    horizontalKernel[right].X = -offsetX;

                    verticalKernel[left].Y = offsetY;
                    verticalKernel[right].Y = -offsetY;
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

                horizontalKernel[0].Z = weight;
                verticalKernel[0].Z = weight;

                totalWeight += weight;

                for (int i = 0; i < kernelSize / 2; i++)
                {
                    int baseIndex = i * 2;
                    int left = baseIndex + 1;
                    int right = baseIndex + 2;

                    weight = MathHelper.CalculateGaussian(sigma, i + 1);
                    totalWeight += weight * 2;

                    horizontalKernel[left].Z = weight;
                    horizontalKernel[right].Z = weight;

                    verticalKernel[left].Z = weight;
                    verticalKernel[right].Z = weight;
                }

                float inverseTotalWeights = 1.0f / totalWeight;
                for (int i = 0; i < kernelSize; i++)
                {
                    horizontalKernel[i].Z *= inverseTotalWeights;
                    verticalKernel[i].Z *= inverseTotalWeights;
                }

                dirtyFlags &= ~DirtyFlags.KernelWeights;
                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        #region IDisposable

        bool disposed;

        ~GaussianBlurCore()
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
