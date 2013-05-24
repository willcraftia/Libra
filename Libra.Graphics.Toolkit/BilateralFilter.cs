#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class BilateralFilter : IGaussianFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.BilateralFilterPS);
            }
        }

        #endregion

        #region ParametersPerObject

        [StructLayout(LayoutKind.Explicit, Size = 16 + 16 * MaxKernelSize)]
        struct ParametersPerObject
        {
            [FieldOffset(0)]
            public float ColorSigma;

            [FieldOffset(4)]
            public float KernelSize;

            // X:   重み
            // YZW: 整列用ダミー
            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxKernelSize)]
            public Vector4[] SpaceWeights;
        }

        #endregion

        #region ParametersPerRenderTarget

        [StructLayout(LayoutKind.Sequential, Size = 16 * MaxKernelSize)]
        struct ParametersPerRenderTarget
        {
            // XY:  テクセル オフセット
            // ZW:  整列用ダミー
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxKernelSize)]
            public Vector4[] Offsets;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObject         = (1 << 0),
            ConstantBufferPerRenderTarget   = (1 << 1),
            SpaceWeights                    = (1 << 2),
            Offsets                         = (1 << 3),
        }

        #endregion

        public const int MaxRadius = 7;

        public const int MaxKernelSize = MaxRadius * 2 + 1;

        public const int DefaultRadius = 7;

        public const float DefaultSpaceSigma = 4.0f;

        public const float DefaultColorSigma = 0.2f;

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ParametersPerObject parametersPerObject;

        ParametersPerRenderTarget parametersPerRenderTargetH;

        ParametersPerRenderTarget parametersPerRenderTargetV;

        ConstantBuffer constantBufferPerObject;

        ConstantBuffer constantBuffferPerRenderTargetH;

        ConstantBuffer constantBuffferPerRenderTargetV;

        int radius;

        float spaceSigma;

        int viewportWidth;

        int viewportHeight;

        DirtyFlags dirtyFlags;

        public GaussianFilterDirection Direction { get; set; }

        public int Radius
        {
            get { return radius; }
            set
            {
                if (value < 1 || MaxRadius < value) throw new ArgumentOutOfRangeException("value");

                radius = value;
                parametersPerObject.KernelSize = radius * 2 + 1;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float SpaceSigma
        {
            get { return spaceSigma; }
            set
            {
                if (value < float.Epsilon) throw new ArgumentOutOfRangeException("value");

                spaceSigma = value;

                dirtyFlags |= DirtyFlags.SpaceWeights;
            }
        }

        public float ColorSigma
        {
            get { return parametersPerObject.ColorSigma; }
            set
            {
                if (value < float.Epsilon) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.ColorSigma = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public bool Enabled { get; set; }

        public BilateralFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<BilateralFilter, SharedDeviceResource>();

            constantBufferPerObject = device.CreateConstantBuffer();
            constantBufferPerObject.Initialize<ParametersPerObject>();

            constantBuffferPerRenderTargetH = device.CreateConstantBuffer();
            constantBuffferPerRenderTargetH.Initialize<ParametersPerRenderTarget>();

            constantBuffferPerRenderTargetV = device.CreateConstantBuffer();
            constantBuffferPerRenderTargetV.Initialize<ParametersPerRenderTarget>();

            parametersPerObject.SpaceWeights = new Vector4[MaxKernelSize];
            parametersPerRenderTargetH.Offsets = new Vector4[MaxKernelSize];
            parametersPerRenderTargetV.Offsets = new Vector4[MaxKernelSize];

            radius = DefaultRadius;
            spaceSigma = DefaultSpaceSigma;
            viewportWidth = 1;
            viewportHeight = 1;

            parametersPerObject.KernelSize = radius * 2 + 1;
            parametersPerObject.ColorSigma = DefaultColorSigma;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerObject |
                DirtyFlags.ConstantBufferPerRenderTarget |
                DirtyFlags.SpaceWeights |
                DirtyFlags.Offsets;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var viewport = context.Viewport;
            int currentWidth = (int) viewport.Width;
            int currentHeight = (int) viewport.Height;

            if (currentWidth != viewportWidth || currentHeight != viewportHeight)
            {
                viewportWidth = currentWidth;
                viewportHeight = currentHeight;

                dirtyFlags |= DirtyFlags.Offsets;
            }

            SetSpaceWeights();
            SetOffsets();

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObject) != 0)
            {
                constantBufferPerObject.SetData(context, parametersPerObject);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObject;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerRenderTarget) != 0)
            {
                constantBuffferPerRenderTargetH.SetData(context, parametersPerRenderTargetH);
                constantBuffferPerRenderTargetV.SetData(context, parametersPerRenderTargetV);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerRenderTarget;
            }

            context.PixelShaderConstantBuffers[0] = constantBufferPerObject;

            switch (Direction)
            {
                case GaussianFilterDirection.Horizon:
                    context.PixelShaderConstantBuffers[1] = constantBuffferPerRenderTargetH;
                    break;
                case GaussianFilterDirection.Vertical:
                    context.PixelShaderConstantBuffers[1] = constantBuffferPerRenderTargetV;
                    break;
                default:
                    throw new InvalidOperationException("Unknown direction: " + Direction);
            }

            context.PixelShader = sharedDeviceResource.PixelShader;
        }

        void SetSpaceWeights()
        {
            if ((dirtyFlags & DirtyFlags.SpaceWeights) != 0)
            {
                // 空間の重みにはガウス関数の出力をそのまま設定する。
                // シェーダ内で色の重み付けを考慮して最終的な重みを正規化するため、
                // ここで空間の重みを正規化する必要は無い (行なっても良いが冗長)。

                var weight = MathHelper.CalculateGaussian(spaceSigma, 0);

                parametersPerObject.SpaceWeights[0].X = weight;

                for (int i = 0; i < MaxKernelSize / 2; i++)
                {
                    int baseIndex = i * 2;
                    int left = baseIndex + 1;
                    int right = baseIndex + 2;

                    weight = MathHelper.CalculateGaussian(spaceSigma, i + 1);

                    parametersPerObject.SpaceWeights[left].X = weight;
                    parametersPerObject.SpaceWeights[right].X = weight;
                }

                dirtyFlags &= ~DirtyFlags.SpaceWeights;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        void SetOffsets()
        {
            if ((dirtyFlags & DirtyFlags.Offsets) != 0)
            {
                var dx = 1.0f / (float) viewportWidth;
                var dy = 1.0f / (float) viewportHeight;

                parametersPerRenderTargetH.Offsets[0].X = 0.0f;
                parametersPerRenderTargetV.Offsets[0].Y = 0.0f;

                for (int i = 0; i < MaxKernelSize / 2; i++)
                {
                    int baseIndex = i * 2;
                    int left = baseIndex + 1;
                    int right = baseIndex + 2;

                    float sampleOffset = i * 2 + 1.5f;
                    var offsetX = dx * sampleOffset;
                    var offsetY = dy * sampleOffset;

                    parametersPerRenderTargetH.Offsets[left].X = offsetX;
                    parametersPerRenderTargetH.Offsets[right].X = -offsetX;

                    parametersPerRenderTargetV.Offsets[left].Y = offsetY;
                    parametersPerRenderTargetV.Offsets[right].Y = -offsetY;
                }

                dirtyFlags &= ~DirtyFlags.Offsets;
                dirtyFlags |= DirtyFlags.ConstantBufferPerRenderTarget;
            }
        }

        #region IDisposable

        bool disposed;

        ~BilateralFilter()
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
                constantBufferPerObject.Dispose();
                constantBuffferPerRenderTargetH.Dispose();
                constantBuffferPerRenderTargetV.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
