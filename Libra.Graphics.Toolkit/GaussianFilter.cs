#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    /// <summary>
    /// ガウシアン フィルタです。
    /// </summary>
    /// <remarks>
    /// このクラスは、SpriteBatch あるいは FullscreenQuad の頂点シェーダの利用を前提としています。
    /// </remarks>
    public sealed class GaussianFilter : IGaussianFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.GaussianFilterPS);
            }
        }

        #endregion

        #region ParametersPerObject

        [StructLayout(LayoutKind.Explicit, Size = 16 + 16 * MaxKernelSize)]
        struct ParametersPerObject
        {
            [FieldOffset(0)]
            public float KernelSize;

            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxKernelSize)]
            public Vector4[] Weights;
        }

        #endregion

        #region ParametersPerRenderTarget

        [StructLayout(LayoutKind.Sequential, Size = 16 * MaxKernelSize)]
        struct ParametersPerRenderTarget
        {
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
            Weights                         = (1 << 2),
            Offsets                         = (1 << 3),
        }

        #endregion

        public const int MaxRadius = 7;

        public const int MaxKernelSize = MaxRadius * 2 + 1;

        public const int DefaultRadius = 7;

        public const float DefaultSigma = 4.0f;

        SharedDeviceResource sharedDeviceResource;

        ParametersPerObject parametersPerObject;

        ParametersPerRenderTarget parametersPerRenderTargetH;

        ParametersPerRenderTarget parametersPerRenderTargetV;

        ConstantBuffer constantBufferPerObject;

        ConstantBuffer constantBufferPerRenderTargetH;

        ConstantBuffer constantBufferPerRenderTargetV;

        int radius;

        float sigma;

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

                if (radius == value) return;

                radius = value;
                parametersPerObject.KernelSize = radius * 2 + 1;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float Sigma
        {
            get { return sigma; }
            set
            {
                if (value < float.Epsilon) throw new ArgumentOutOfRangeException("value");

                if (sigma == value) return;

                sigma = value;

                dirtyFlags |= DirtyFlags.Weights;
            }
        }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public GaussianFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<GaussianFilter, SharedDeviceResource>();

            constantBufferPerObject = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObject.Initialize<ParametersPerObject>();

            constantBufferPerRenderTargetH = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerRenderTargetH.Initialize<ParametersPerRenderTarget>();

            constantBufferPerRenderTargetV = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerRenderTargetV.Initialize<ParametersPerRenderTarget>();

            parametersPerObject.Weights = new Vector4[MaxKernelSize];
            parametersPerRenderTargetH.Offsets = new Vector4[MaxKernelSize];
            parametersPerRenderTargetV.Offsets = new Vector4[MaxKernelSize];

            radius = DefaultRadius;
            sigma = DefaultSigma;
            viewportWidth = 1;
            viewportHeight = 1;

            parametersPerObject.KernelSize = radius * 2 + 1;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerObject |
                DirtyFlags.ConstantBufferPerRenderTarget |
                DirtyFlags.Weights |
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

            SetWeights();
            SetOffsets();

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObject) != 0)
            {
                DeviceContext.SetData(constantBufferPerObject, parametersPerObject);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObject;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerRenderTarget) != 0)
            {
                DeviceContext.SetData(constantBufferPerRenderTargetH, parametersPerRenderTargetH);
                DeviceContext.SetData(constantBufferPerRenderTargetV, parametersPerRenderTargetV);

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
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
        }

        void SetWeights()
        {
            if ((dirtyFlags & DirtyFlags.Weights) != 0)
            {
                var totalWeight = 0.0f;

                var weight = MathHelper.CalculateGaussian(sigma, 0);

                parametersPerObject.Weights[0].X = weight;

                totalWeight += weight;

                for (int i = 0; i < MaxKernelSize / 2; i++)
                {
                    int baseIndex = i * 2;
                    int left = baseIndex + 1;
                    int right = baseIndex + 2;

                    weight = MathHelper.CalculateGaussian(sigma, i + 1);
                    totalWeight += weight * 2;

                    parametersPerObject.Weights[left].X = weight;
                    parametersPerObject.Weights[right].X = weight;
                }

                float inverseTotalWeights = 1.0f / totalWeight;
                for (int i = 0; i < MaxKernelSize; i++)
                {
                    parametersPerObject.Weights[i].X *= inverseTotalWeights;
                }

                dirtyFlags &= ~DirtyFlags.Weights;
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

        ~GaussianFilter()
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
