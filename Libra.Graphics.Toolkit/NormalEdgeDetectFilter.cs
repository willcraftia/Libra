#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class NormalEdgeDetectFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.NormalEdgeDetectFilterPS);
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
            Offsets                         = (1 << 1),
        }

        #endregion

        const int KernelSize = 4;

        static readonly Vector2[] Offsets =
        {
            new Vector2( 0,  1),
            new Vector2( 1,  0),
            new Vector2( 0, -1),
            new Vector2(-1,  0),
        };

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerRenderTarget;

        ParametersPerRenderTarget parametersPerRenderTarget;

        int viewportWidth;

        int viewportHeight;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public ShaderResourceView NormalMap { get; set; }

        public SamplerState NormalMapSampler { get; set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture
        {
            get { return null; }
            set { }
        }

        public SamplerState TextureSampler
        {
            get { return null; }
            set { }
        }

        public NormalEdgeDetectFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<NormalEdgeDetectFilter, SharedDeviceResource>();

            constantBufferPerRenderTarget = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerRenderTarget.Initialize<ParametersPerRenderTarget>();

            parametersPerRenderTarget.Offsets = new Vector4[KernelSize];

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
                constantBufferPerRenderTarget.SetData(DeviceContext, parametersPerRenderTarget);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerRenderTarget;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerRenderTarget;
            DeviceContext.PixelShaderResources[1] = NormalMap;
            DeviceContext.PixelShaderSamplers[1] = NormalMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~NormalEdgeDetectFilter()
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
            }

            disposed = true;
        }

        #endregion
    }
}
