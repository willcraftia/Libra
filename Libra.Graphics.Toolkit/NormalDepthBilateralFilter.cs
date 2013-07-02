#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class NormalDepthBilateralFilter : IGaussianFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.NormalDepthBilateralFilterPS);
            }
        }

        #endregion

        #region ParametersPerObject

        [StructLayout(LayoutKind.Explicit, Size = 16 + 16 * MaxKernelSize)]
        struct ParametersPerObject
        {
            [FieldOffset(0)]
            public float DepthSigma;

            [FieldOffset(4)]
            public float NormalSigma;

            [FieldOffset(8)]
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
            Offsets                         = (1 << 3)
        }

        #endregion

        public const int MaxRadius = 7;

        public const int MaxKernelSize = MaxRadius * 2 + 1;

        public const int DefaultRadius = 7;

        public const float DefaultSpaceSigma = 4.0f;

        public const float DefaultDepthSigma = 3.0f;

        public const float DefaultNormalSigma = 1.0f;

        SharedDeviceResource sharedDeviceResource;

        ParametersPerObject parametersPerObject;

        ParametersPerRenderTarget parametersPerRenderTargetH;

        ParametersPerRenderTarget parametersPerRenderTargetV;

        ConstantBuffer constantBufferPerObject;

        ConstantBuffer constantBufferPerRenderTargetH;

        ConstantBuffer constantBufferPerRenderTargetV;

        int radius;

        float spaceSigma;

        int viewportWidth;

        int viewportHeight;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

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

        public float DepthSigma
        {
            get { return parametersPerObject.DepthSigma; }
            set
            {
                if (value < float.Epsilon) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.DepthSigma = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float NormalSigma
        {
            get { return parametersPerObject.NormalSigma; }
            set
            {
                if (value < float.Epsilon) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.NormalSigma = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public ShaderResourceView LinearDepthMap { get; set; }

        public ShaderResourceView NormalMap { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public SamplerState NormalMapSampler { get; set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public NormalDepthBilateralFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<NormalDepthBilateralFilter, SharedDeviceResource>();

            constantBufferPerObject = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObject.Initialize<ParametersPerObject>();

            constantBufferPerRenderTargetH = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerRenderTargetH.Initialize<ParametersPerRenderTarget>();

            constantBufferPerRenderTargetV = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerRenderTargetV.Initialize<ParametersPerRenderTarget>();

            parametersPerObject.SpaceWeights = new Vector4[MaxKernelSize];
            parametersPerRenderTargetH.Offsets = new Vector4[MaxKernelSize];
            parametersPerRenderTargetV.Offsets = new Vector4[MaxKernelSize];

            radius = DefaultRadius;
            spaceSigma = DefaultSpaceSigma;
            viewportWidth = 1;
            viewportHeight = 1;

            parametersPerObject.KernelSize = radius * 2 + 1;
            parametersPerObject.DepthSigma = DefaultDepthSigma;
            parametersPerObject.NormalSigma = DefaultNormalSigma;

            LinearDepthMapSampler = SamplerState.PointClamp;
            NormalMapSampler = SamplerState.PointClamp;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerObject |
                DirtyFlags.ConstantBufferPerRenderTarget |
                DirtyFlags.SpaceWeights |
                DirtyFlags.Offsets;
        }

        public void Apply()
        {
            var viewport = DeviceContext.Viewport;
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
                constantBufferPerObject.SetData(DeviceContext, parametersPerObject);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObject;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerRenderTarget) != 0)
            {
                constantBufferPerRenderTargetH.SetData(DeviceContext, parametersPerRenderTargetH);
                constantBufferPerRenderTargetV.SetData(DeviceContext, parametersPerRenderTargetV);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerRenderTarget;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerObject;

            switch (Direction)
            {
                case GaussianFilterDirection.Horizon:
                    DeviceContext.PixelShaderConstantBuffers[1] = constantBufferPerRenderTargetH;
                    break;
                case GaussianFilterDirection.Vertical:
                    DeviceContext.PixelShaderConstantBuffers[1] = constantBufferPerRenderTargetV;
                    break;
                default:
                    throw new InvalidOperationException("Unknown direction: " + Direction);
            }

            DeviceContext.PixelShaderResources[0] = Texture;
            DeviceContext.PixelShaderResources[1] = LinearDepthMap;
            DeviceContext.PixelShaderResources[2] = NormalMap;
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
            DeviceContext.PixelShaderSamplers[1] = LinearDepthMapSampler;
            DeviceContext.PixelShaderSamplers[2] = NormalMapSampler;
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

        ~NormalDepthBilateralFilter()
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
                constantBufferPerRenderTargetH.Dispose();
                constantBufferPerRenderTargetV.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
