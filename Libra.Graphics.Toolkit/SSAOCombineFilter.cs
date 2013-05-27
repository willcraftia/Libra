#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class SSAOCombineFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.SSAOCombineFilterPS);
            }
        }

        #endregion

        #region ParametersPerObject

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        struct ParametersPerObject
        {
            public Vector3 ShadowColor;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObject = (1 << 0)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerObject;

        ParametersPerObject parametersPerObject;

        DirtyFlags dirtyFlags;

        public Vector3 ShadowColor
        {
            get { return parametersPerObject.ShadowColor; }
            set
            {
                parametersPerObject.ShadowColor = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        /// <summary>
        /// 環境光閉塞マップを取得または設定します。
        /// </summary>
        public ShaderResourceView SSAOMap { get; set; }

        public SamplerState SSAOMapSampler { get; set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public SSAOCombineFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<SSAOCombineFilter, SharedDeviceResource>();

            constantBufferPerObject = device.CreateConstantBuffer();
            constantBufferPerObject.Initialize<ParametersPerObject>();

            parametersPerObject.ShadowColor = Vector3.Zero;

            Enabled = true;

            dirtyFlags = DirtyFlags.ConstantBufferPerObject;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObject) != 0)
            {
                constantBufferPerObject.SetData(context, parametersPerObject);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObject;
            }

            context.PixelShader = sharedDeviceResource.PixelShader;
            context.PixelShaderConstantBuffers[0] = constantBufferPerObject;
            context.PixelShaderResources[0] = Texture;
            context.PixelShaderResources[1] = SSAOMap;
            context.PixelShaderSamplers[1] = SSAOMapSampler;
            context.PixelShaderSamplers[0] = TextureSampler;
        }

        #region IDisposable

        bool disposed;

        ~SSAOCombineFilter()
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
