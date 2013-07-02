#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class HeightToNormalConverter : IEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.HeightToNormalFilterPS);
            }
        }

        #endregion

        #region ParametersPerRenderTarget

        [StructLayout(LayoutKind.Sequential, Size = 16 * KernelSize)]
        struct ParametersPerRenderTarget
        {
            // XY:  テクセル オフセット
            // ZW:  整列用ダミー
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = KernelSize)]
            public Vector4[] Offsets;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerRenderTarget   = (1 << 0),
            Offsets                         = (1 << 1),
        }

        #endregion

        public const int KernelSize = 4;

        static readonly Vector2[] Offsets =
        {
            new Vector2(-1.5f,  0.0f),
            new Vector2( 1.5f,  0.0f),
            new Vector2( 0.0f, -1.5f),
            new Vector2( 0.0f,  1.5f),
        };

        SharedDeviceResource sharedDeviceResource;

        ParametersPerRenderTarget parametersPerRenderTarget;

        ConstantBuffer constantBufferPerRenderTarget;

        int viewportWidth;

        int viewportHeight;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public ShaderResourceView HeightMap { get; set; }

        public SamplerState HeightMapSampler { get; set; }

        public HeightToNormalConverter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<HeightToNormalConverter, SharedDeviceResource>();

            constantBufferPerRenderTarget = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerRenderTarget.Initialize<ParametersPerRenderTarget>();
            
            viewportWidth = 1;
            viewportHeight = 1;

            parametersPerRenderTarget.Offsets = new Vector4[KernelSize];

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
                constantBufferPerRenderTarget.SetData(DeviceContext, parametersPerRenderTarget);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerRenderTarget;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerRenderTarget;
            DeviceContext.PixelShaderResources[0] = HeightMap;
            DeviceContext.PixelShaderSamplers[0] = HeightMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~HeightToNormalConverter()
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
