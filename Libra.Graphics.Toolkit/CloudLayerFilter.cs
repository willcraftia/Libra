#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class CloudLayerFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.CloudLayerFilterPS);
            }
        }

        #endregion

        #region ParametersPerObject

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        struct ParametersPerObject
        {
            [FieldOffset(0)]
            public Vector3 CloudColor;

            // テクセル
            [FieldOffset(16)]
            public Vector2 Offset;

            [FieldOffset(24)]
            public float LightAbsorption;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObject = (1 << 0),
            Offset                  = (1 << 1)
        }

        #endregion

        SharedDeviceResource sharedDeviceResource;

        ParametersPerObject parametersPerObject;

        ConstantBuffer constantBufferPerObject;

        Vector2 pixelOffset;

        int viewportWidth;

        int viewportHeight;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public Vector2 PixelOffset
        {
            get { return pixelOffset; }
            set
            {
                pixelOffset = value;

                dirtyFlags |= DirtyFlags.Offset;
            }
        }

        public ShaderResourceView DensityMap { get; set; }

        public SamplerState DensityMapSampler { get; set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public CloudLayerFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<CloudLayerFilter, SharedDeviceResource>();

            constantBufferPerObject = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObject.Initialize<ParametersPerObject>();

            parametersPerObject.CloudColor = Vector3.One;
            parametersPerObject.Offset = Vector2.Zero;
            parametersPerObject.LightAbsorption = 0.1f;

            viewportWidth = 1;
            viewportHeight = 1;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerObject |
                DirtyFlags.Offset;
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

                dirtyFlags |= DirtyFlags.Offset;
            }

            if ((dirtyFlags & DirtyFlags.Offset) != 0)
            {
                parametersPerObject.Offset.X = pixelOffset.X / (float) viewportWidth;
                parametersPerObject.Offset.Y = pixelOffset.Y / (float) viewportHeight;

                dirtyFlags &= ~DirtyFlags.Offset;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObject) != 0)
            {
                constantBufferPerObject.SetData(DeviceContext, parametersPerObject);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObject;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerObject;
            DeviceContext.PixelShaderResources[0] = Texture;
            DeviceContext.PixelShaderResources[1] = DensityMap;
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
            DeviceContext.PixelShaderSamplers[1] = DensityMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~CloudLayerFilter()
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
            }

            disposed = true;
        }

        #endregion
    }
}
