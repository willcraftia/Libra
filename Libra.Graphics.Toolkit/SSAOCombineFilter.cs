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

        #region ParametersPerShader

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        struct ParametersPerShader
        {
            public Vector3 ShadowColor;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerShader = (1 << 0)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerShader;

        ParametersPerShader parametersPerShader;

        DirtyFlags dirtyFlags;

        public Vector3 ShadowColor
        {
            get { return parametersPerShader.ShadowColor; }
            set
            {
                parametersPerShader.ShadowColor = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerShader;
            }
        }

        /// <summary>
        /// 環境光閉塞マップを取得または設定します。
        /// </summary>
        public ShaderResourceView SSAOMap { get; set; }

        public SamplerState SSAOMapSampler { get; set; }

        public bool Enabled { get; set; }

        public SSAOCombineFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<SSAOCombineFilter, SharedDeviceResource>();

            constantBufferPerShader = device.CreateConstantBuffer();
            constantBufferPerShader.Initialize<ParametersPerShader>();

            parametersPerShader.ShadowColor = Vector3.Zero;

            SSAOMapSampler = SamplerState.LinearClamp;

            Enabled = true;

            dirtyFlags = DirtyFlags.ConstantBufferPerShader;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.ConstantBufferPerShader) != 0)
            {
                constantBufferPerShader.SetData(context, parametersPerShader);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerShader;
            }

            context.PixelShaderConstantBuffers[0] = constantBufferPerShader;
            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[1] = SSAOMap;
            context.PixelShaderSamplers[1] = SSAOMapSampler;
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
                constantBufferPerShader.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
