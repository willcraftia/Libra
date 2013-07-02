#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class RadialFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.RadialFilterPS);
            }
        }

        #endregion

        #region ParametersPerShader

        [StructLayout(LayoutKind.Explicit, Size = 16 + 16 * MaxKernelSize)]
        struct ParametersPerShader
        {
            [FieldOffset(0)]
            public int KernelSize;

            // X:   重み
            // YZW: 整列用ダミー
            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxKernelSize)]
            public Vector4[] Weights;
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

        #region ParametersPerFrame

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        struct ParametersPerFrame
        {
            [FieldOffset(0)]
            public Vector2 Center;

            [FieldOffset(8)]
            public float Strength;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerShader         = (1 << 0),
            ConstantBufferPerRenderTarget   = (1 << 1),
            ConstantBufferPerFrame          = (1 << 2),
            Weights                         = (1 << 3),
            Offsets                         = (1 << 4),
        }

        #endregion

        public const int MaxKernelSize = 32;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerShader;

        ConstantBuffer constantBufferPerRenderTarget;

        ConstantBuffer constantBufferPerFrame;

        ParametersPerShader parametersPerShader;

        ParametersPerRenderTarget parametersPerRenderTarget;

        ParametersPerFrame parametersPerFrame;

        float sigma;

        int viewportWidth;

        int viewportHeight;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public int SampleCount
        {
            get { return parametersPerShader.KernelSize; }
            set
            {
                if ((uint) MaxKernelSize < (uint) value) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.KernelSize = value;

                dirtyFlags |= DirtyFlags.Offsets | DirtyFlags.Weights | DirtyFlags.ConstantBufferPerShader;
            }
        }

        public float Sigma
        {
            get { return sigma; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                sigma = value;

                dirtyFlags |= DirtyFlags.Weights;
            }
        }

        public Vector2 Center
        {
            get { return parametersPerFrame.Center; }
            set
            {
                parametersPerFrame.Center = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerFrame;
            }
        }

        public float Strength
        {
            get { return parametersPerFrame.Strength; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerFrame.Strength = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerFrame;
            }
        }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public RadialFilter(DeviceContext deviceContext)
        {
            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<RadialFilter, SharedDeviceResource>();

            constantBufferPerShader = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerShader.Initialize<ParametersPerShader>();

            constantBufferPerRenderTarget = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerRenderTarget.Initialize<ParametersPerShader>();

            constantBufferPerFrame = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerFrame.Initialize<ParametersPerShader>();

            sigma = 10.0f;

            parametersPerShader.KernelSize = 10;
            parametersPerShader.Weights = new Vector4[MaxKernelSize];

            parametersPerRenderTarget.Offsets = new Vector4[MaxKernelSize];

            parametersPerFrame.Center = new Vector2(0.5f);
            parametersPerFrame.Strength = 10;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerShader |
                DirtyFlags.ConstantBufferPerRenderTarget |
                DirtyFlags.ConstantBufferPerFrame |
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

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerShader) != 0)
            {
                constantBufferPerShader.SetData(DeviceContext, parametersPerShader);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerShader;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerRenderTarget) != 0)
            {
                constantBufferPerRenderTarget.SetData(DeviceContext, parametersPerRenderTarget);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerRenderTarget;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerFrame) != 0)
            {
                constantBufferPerFrame.SetData(DeviceContext, parametersPerFrame);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerFrame;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerShader;
            DeviceContext.PixelShaderConstantBuffers[1] = constantBufferPerRenderTarget;
            DeviceContext.PixelShaderConstantBuffers[2] = constantBufferPerFrame;
            DeviceContext.PixelShaderResources[0] = Texture;
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
        }

        void SetWeights()
        {
            if ((dirtyFlags & DirtyFlags.Weights) != 0)
            {
                var totalWeight = 0.0f;

                for (int i = 0; i < parametersPerShader.KernelSize; i++)
                {
                    var weight = MathHelper.CalculateGaussian(sigma, i);

                    totalWeight += weight;
                    parametersPerShader.Weights[i].X = weight;
                }

                float invertTotalWeights = 1.0f / totalWeight;
                for (int i = 0; i < parametersPerShader.KernelSize; i++)
                {
                    parametersPerShader.Weights[i].X *= invertTotalWeights;
                }

                dirtyFlags &= ~DirtyFlags.Weights;
                dirtyFlags |= DirtyFlags.ConstantBufferPerShader;
            }
        }

        void SetOffsets()
        {
            if ((dirtyFlags & DirtyFlags.Offsets) != 0)
            {
                var dx = 1.0f / (float) viewportWidth;
                var dy = 1.0f / (float) viewportHeight;

                parametersPerRenderTarget.Offsets[0].X = 0.0f;
                parametersPerRenderTarget.Offsets[0].Y = 0.0f;

                for (int i = 1; i < parametersPerShader.KernelSize; i++)
                {
                    float sampleOffset = i + 0.5f;
                    parametersPerRenderTarget.Offsets[i].X = dx * sampleOffset;
                    parametersPerRenderTarget.Offsets[i].Y = dy * sampleOffset;
                }

                dirtyFlags &= ~DirtyFlags.Offsets;
                dirtyFlags |= DirtyFlags.ConstantBufferPerRenderTarget;
            }
        }

        #region IDisposable

        bool disposed;

        ~RadialFilter()
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
                constantBufferPerShader.Dispose();
                constantBufferPerRenderTarget.Dispose();
                constantBufferPerFrame.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
