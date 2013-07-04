#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class DownFilter : IFilterEffect, IFilterEffectScale, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.DownFilterPS);
            }
        }

        #endregion

        #region ParametersPerRenderTarget

        [StructLayout(LayoutKind.Sequential, Size = 16 * KernelSize)]
        public struct ParametersPerRenderTarget
        {
            // XY: テクセル オフセット
            // ZW: 整列用ダミー
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = KernelSize)]
            public Vector4[] Offsets;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerRenderTarget   = (1 << 0),
            Offsets                         = (1 << 1)
        }

        #endregion

        const int KernelSize = 16;

        static readonly Vector2[] Offsets =
        {
            new Vector2( 1.5f, -1.5f),
            new Vector2( 1.5f, -0.5f),
            new Vector2( 1.5f,  0.5f),
            new Vector2( 1.5f,  1.5f),

            new Vector2( 0.5f, -1.5f),
            new Vector2( 0.5f, -0.5f),
            new Vector2( 0.5f,  0.5f),
            new Vector2( 0.5f,  1.5f),

            new Vector2(-0.5f, -1.5f),
            new Vector2(-0.5f, -0.5f),
            new Vector2(-0.5f,  0.5f),
            new Vector2(-0.5f,  1.5f),

            new Vector2(-1.5f, -1.5f),
            new Vector2(-1.5f, -0.5f),
            new Vector2(-1.5f,  0.5f),
            new Vector2(-1.5f,  1.5f),
        };

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerRenderTarget;

        ParametersPerRenderTarget parametersPerRenderTarget;

        int viewportWidth;

        int viewportHeight;

        DirtyFlags dirtyFlags;

        float widthScale;

        float heightScale;

        public DeviceContext DeviceContext { get; private set; }

        public float WidthScale
        {
            get { return widthScale; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                widthScale = value;
            }
        }

        public float HeightScale
        {
            get { return heightScale; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                heightScale = value;
            }
        }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public DownFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<DownFilter, SharedDeviceResource>();

            constantBufferPerRenderTarget = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerRenderTarget.Initialize<ParametersPerRenderTarget>();

            parametersPerRenderTarget.Offsets = new Vector4[KernelSize];

            widthScale = 0.25f;
            heightScale = 0.25f;

            Enabled = true;

            dirtyFlags = DirtyFlags.Offsets | DirtyFlags.ConstantBufferPerRenderTarget;
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

            if ((dirtyFlags & DirtyFlags.Offsets) != 0)
            {
                for (int i = 0; i < KernelSize; i++)
                {
                    parametersPerRenderTarget.Offsets[i].X = Offsets[i].X / (float) viewportWidth;
                    parametersPerRenderTarget.Offsets[i].Y = Offsets[i].Y / (float) viewportHeight;
                }

                dirtyFlags &= ~DirtyFlags.Offsets;
                dirtyFlags |= DirtyFlags.ConstantBufferPerRenderTarget;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerRenderTarget) != 0)
            {
                DeviceContext.SetData(constantBufferPerRenderTarget, parametersPerRenderTarget);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerRenderTarget;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerRenderTarget;
            DeviceContext.PixelShaderResources[0] = Texture;
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
        }

        #region IDisposable

        bool disposed;

        ~DownFilter()
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
                constantBufferPerRenderTarget.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
