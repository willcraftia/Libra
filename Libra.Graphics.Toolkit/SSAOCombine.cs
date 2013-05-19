#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class SSAOCombine : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.SSAOCombinePS);
            }
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        struct Constants
        {
            public Vector3 ShadowColor;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            Constants   = (1 << 0)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

        DirtyFlags dirtyFlags;

        public Vector3 ShadowColor
        {
            get { return constants.ShadowColor; }
            set
            {
                constants.ShadowColor = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        /// <summary>
        /// 環境光閉塞マップを取得または設定します。
        /// </summary>
        public ShaderResourceView SSAOMap { get; set; }

        public SamplerState SSAOMapSampler { get; set; }

        public bool Enabled { get; set; }

        public SSAOCombine(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<SSAOCombine, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            constants.ShadowColor = Vector3.Zero;

            SSAOMapSampler = SamplerState.LinearClamp;

            Enabled = true;

            dirtyFlags = DirtyFlags.Constants;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constantBuffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            context.PixelShaderConstantBuffers[0] = constantBuffer;
            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[1] = SSAOMap;
            context.PixelShaderSamplers[1] = SSAOMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~SSAOCombine()
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
                constantBuffer.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
