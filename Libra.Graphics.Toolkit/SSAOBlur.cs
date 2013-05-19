#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class SSAOBlur : IGaussianFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.SSAOBlurPS);
            }
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Explicit, Size = 16 + 16 * MaxKernelSize)]
        struct Constants
        {
            [FieldOffset(0)]
            public float DepthSigma;

            [FieldOffset(4)]
            public float NormalSigma;

            [FieldOffset(8)]
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
            KernelSize          = (1 << 0),
            KernelOffsets       = (1 << 1),
            KernelSpaceWeights  = (1 << 2),
            Constants           = (1 << 3)
        }

        #endregion

        public const int MaxRadius = 7;

        public const int MaxKernelSize = MaxRadius * 2 + 1;

        public const int DefaultRadius = 7;

        public const float DefaultSpaceSigma = 4.0f;

        public const float DefaultDepthSigma = 3.0f;

        public const float DefaultNormalSigma = 1.0f;

        Device device;

        SharedDeviceResource sharedDeviceResource;

        Constants constants;

        ConstantBuffer constantBufferH;

        ConstantBuffer constantBuffferV;

        int kernelSize;

        Vector4[] kernelH;

        Vector4[] kernelV;

        int radius;

        float spaceSigma;

        int width;

        int height;

        DirtyFlags dirtyFlags;

        public GaussianFilterDirection Direction { get; set; }

        public int Radius
        {
            get { return radius; }
            set
            {
                if (value < 1 || MaxRadius < value) throw new ArgumentOutOfRangeException("value");

                if (radius == value) return;

                radius = value;

                dirtyFlags |= DirtyFlags.KernelSize | DirtyFlags.KernelSpaceWeights;
            }
        }

        public float SpaceSigma
        {
            get { return spaceSigma; }
            set
            {
                if (value < float.Epsilon) throw new ArgumentOutOfRangeException("value");

                if (spaceSigma == value) return;

                spaceSigma = value;

                dirtyFlags |= DirtyFlags.KernelSpaceWeights;
            }
        }

        public float DepthSigma
        {
            get { return constants.DepthSigma; }
            set
            {
                if (value < float.Epsilon) throw new ArgumentOutOfRangeException("value");

                if (constants.DepthSigma == value) return;

                constants.DepthSigma = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float NormalSigma
        {
            get { return constants.NormalSigma; }
            set
            {
                if (value < float.Epsilon) throw new ArgumentOutOfRangeException("value");

                if (constants.NormalSigma == value) return;

                constants.NormalSigma = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public ShaderResourceView LinearDepthMap { get; set; }

        public ShaderResourceView NormalMap { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public SamplerState NormalMapSampler { get; set; }

        public bool Enabled { get; set; }

        public SSAOBlur(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<SSAOBlur, SharedDeviceResource>();

            constantBufferH = device.CreateConstantBuffer();
            constantBufferH.Initialize<Constants>();

            constantBuffferV = device.CreateConstantBuffer();
            constantBuffferV.Initialize<Constants>();

            kernelH = new Vector4[MaxKernelSize];
            kernelV = new Vector4[MaxKernelSize];

            radius = DefaultRadius;
            spaceSigma = DefaultSpaceSigma;
            width = 1;
            height = 1;

            constants.DepthSigma = DefaultDepthSigma;
            constants.NormalSigma = DefaultNormalSigma;

            LinearDepthMapSampler = SamplerState.PointClamp;
            NormalMapSampler = SamplerState.PointClamp;

            Enabled = true;

            dirtyFlags |= DirtyFlags.KernelSize | DirtyFlags.KernelOffsets | DirtyFlags.KernelSpaceWeights;
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
            SetKernelSpaceWeights();

            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constants.Kernel = kernelH;
                constantBufferH.SetData(context, constants);

                constants.Kernel = kernelV;
                constantBuffferV.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            // 定数バッファの設定。
            switch (Direction)
            {
                case GaussianFilterDirection.Horizon:
                    context.PixelShaderConstantBuffers[0] = constantBufferH;
                    break;
                case GaussianFilterDirection.Vertical:
                    context.PixelShaderConstantBuffers[0] = constantBuffferV;
                    break;
                default:
                    throw new InvalidOperationException("Unknown direction: " + Direction);
            }

            // ピクセル シェーダの設定。
            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[1] = LinearDepthMap;
            context.PixelShaderResources[2] = NormalMap;
            context.PixelShaderSamplers[1] = LinearDepthMapSampler;
            context.PixelShaderSamplers[2] = NormalMapSampler;
        }

        void SetKernelSize()
        {
            if ((dirtyFlags & DirtyFlags.KernelSize) != 0)
            {
                kernelSize = radius * 2 + 1;
                constants.KernelSize = kernelSize;

                dirtyFlags &= ~DirtyFlags.KernelSize;
                dirtyFlags |= DirtyFlags.KernelOffsets | DirtyFlags.KernelSpaceWeights | DirtyFlags.Constants;
            }
        }

        void SetKernelOffsets()
        {
            if ((dirtyFlags & DirtyFlags.KernelOffsets) != 0)
            {
                var dx = 1.0f / (float) width;
                var dy = 1.0f / (float) height;

                kernelH[0].X = 0.0f;
                kernelV[0].Y = 0.0f;

                for (int i = 0; i < kernelSize / 2; i++)
                {
                    int baseIndex = i * 2;
                    int left = baseIndex + 1;
                    int right = baseIndex + 2;

                    float sampleOffset = i * 2 + 1.5f;
                    var offsetX = dx * sampleOffset;
                    var offsetY = dy * sampleOffset;

                    kernelH[left].X = offsetX;
                    kernelH[right].X = -offsetX;

                    kernelV[left].Y = offsetY;
                    kernelV[right].Y = -offsetY;
                }

                dirtyFlags &= ~DirtyFlags.KernelOffsets;
                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        void SetKernelSpaceWeights()
        {
            if ((dirtyFlags & DirtyFlags.KernelSpaceWeights) != 0)
            {
                // 空間の重みにはガウス関数の出力をそのまま設定する。
                // シェーダ内で色の重み付けを考慮して最終的な重みを正規化するため、
                // ここで空間の重みを正規化する必要は無い (行なっても良いが冗長)。

                var weight = MathHelper.CalculateGaussian(spaceSigma, 0);

                kernelH[0].Z = weight;
                kernelV[0].Z = weight;

                for (int i = 0; i < kernelSize / 2; i++)
                {
                    int baseIndex = i * 2;
                    int left = baseIndex + 1;
                    int right = baseIndex + 2;

                    weight = MathHelper.CalculateGaussian(spaceSigma, i + 1);

                    kernelH[left].Z = weight;
                    kernelH[right].Z = weight;

                    kernelV[left].Z = weight;
                    kernelV[right].Z = weight;
                }

                dirtyFlags &= ~DirtyFlags.KernelSpaceWeights;
                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        #region IDisposable

        bool disposed;

        ~SSAOBlur()
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
                constantBufferH.Dispose();
                constantBuffferV.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
