#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    /// <summary>
    /// 被写界深度を考慮してシーンを合成するフィルタです。
    /// </summary>
    public sealed class DofCombineFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.DofCombineFilterPS);
            }
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        float focusRange;

        float focusDistance;

        bool constantBufferDirty;

        /// <summary>
        /// 焦点範囲を取得または設定します。
        /// </summary>
        public float FocusRange
        {
            get { return focusRange; }
            set
            {
                focusRange = value;

                constantBufferDirty = true;
            }
        }

        /// <summary>
        /// 焦点距離を取得または設定します。
        /// </summary>
        public float FocusDistance
        {
            get { return focusDistance; }
            set
            {
                focusDistance = value;

                constantBufferDirty = true;
            }
        }

        /// <summary>
        /// 通常シーンを取得または設定します。
        /// </summary>
        public ShaderResourceView BaseTexture { get; set; }

        /// <summary>
        /// 深度マップを取得または設定します。
        /// </summary>
        public ShaderResourceView LinearDepthMap { get; set; }

        public SamplerState BaseTextureSampler { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public DofCombineFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<DofCombineFilter, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize(16);

            focusRange = 200.0f;
            focusDistance = 10.0f;

            Enabled = true;

            constantBufferDirty = true;
        }

        public void Apply(DeviceContext context)
        {
            if (constantBufferDirty)
            {
                var data = new Vector4(1.0f / focusRange, focusDistance, 0.0f, 0.0f);

                constantBuffer.SetData(context, data);

                constantBufferDirty = false;
            }

            context.PixelShader = sharedDeviceResource.PixelShader;
            context.PixelShaderConstantBuffers[0] = constantBuffer;
            context.PixelShaderResources[0] = Texture;
            context.PixelShaderResources[1] = BaseTexture;
            context.PixelShaderResources[2] = LinearDepthMap;
            context.PixelShaderSamplers[0] = TextureSampler;
            context.PixelShaderSamplers[1] = BaseTextureSampler;
            context.PixelShaderSamplers[2] = LinearDepthMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~DofCombineFilter()
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
