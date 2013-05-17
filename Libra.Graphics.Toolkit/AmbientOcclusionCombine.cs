#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class AmbientOcclusionCombine : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.AmbientOcclusionCombinePS);
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
        public ShaderResourceView AmbientOcclusionMap { get; set; }

        public SamplerState AmbientOcclusionMapSampler { get; set; }

        public bool Enabled { get; set; }

        public AmbientOcclusionCombine(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<AmbientOcclusionCombine, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            constants.ShadowColor = Vector3.Zero;

            AmbientOcclusionMapSampler = SamplerState.LinearClamp;

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

            context.PixelShaderResources[1] = AmbientOcclusionMap;
            context.PixelShaderSamplers[1] = AmbientOcclusionMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~AmbientOcclusionCombine()
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
