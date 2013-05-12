#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    /// <summary>
    /// 被写界深度を考慮してシーンを合成するポストプロセスです。
    /// </summary>
    public sealed class DofCombine : IPostprocessPass, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.DofCombinePS);
            }
        }

        #endregion

        #region Constants

        struct Constants
        {
            // X = scale
            // Y = distance
            public Vector4 Focus;

            public Matrix InvertProjection;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            FocusScale          = (1 << 0),
            InvertProjection    = (1 << 1),
            Constants           = (1 << 2)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

        float focusRange;

        Matrix projection;

        DirtyFlags dirtyFlags;

        /// <summary>
        /// 焦点距離を取得または設定します。
        /// </summary>
        public float FocusDistance
        {
            get { return constants.Focus.Y; }
            set
            {
                constants.Focus.Y = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        /// <summary>
        /// 焦点範囲を取得または設定します。
        /// </summary>
        public float FocusRange
        {
            get { return focusRange; }
            set
            {
                focusRange = value;

                dirtyFlags |= DirtyFlags.FocusScale;
            }
        }

        /// <summary>
        /// 表示カメラの射影行列を取得または設定します。
        /// </summary>
        public Matrix Projection
        {
            get { return projection; }
            set
            {
                projection = value;

                dirtyFlags |= DirtyFlags.InvertProjection;
            }
        }

        /// <summary>
        /// 通常シーンを取得または設定します。
        /// </summary>
        public ShaderResourceView BaseTexture { get; set; }

        /// <summary>
        /// 深度マップを取得または設定します。
        /// </summary>
        public ShaderResourceView DepthMap { get; set; }

        public SamplerState BaseTextureSampler { get; set; }

        public SamplerState DepthMapSampler { get; set; }

        public bool Enabled { get; set; }

        public DofCombine(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<DofCombine, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            focusRange = 200.0f;
            projection = Matrix.Identity;

            constants.Focus.Y = 10.0f;

            BaseTextureSampler = SamplerState.LinearClamp;
            DepthMapSampler = SamplerState.LinearClamp;

            Enabled = true;

            dirtyFlags = DirtyFlags.FocusScale | DirtyFlags.InvertProjection | DirtyFlags.Constants;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.FocusScale) != 0)
            {
                constants.Focus.X = 1.0f / focusRange;

                dirtyFlags &= ~DirtyFlags.FocusScale;
                dirtyFlags |= DirtyFlags.Constants;
            }
            if ((dirtyFlags & DirtyFlags.InvertProjection) != 0)
            {
                Matrix invertProjection;
                Matrix.Invert(ref projection, out invertProjection);

                Matrix.Transpose(ref invertProjection, out constants.InvertProjection);

                dirtyFlags &= ~DirtyFlags.InvertProjection;
                dirtyFlags |= DirtyFlags.Constants;
            }

            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constantBuffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            context.PixelShaderConstantBuffers[0] = constantBuffer;
            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[1] = BaseTexture;
            context.PixelShaderResources[2] = DepthMap;

            context.PixelShaderSamplers[1] = BaseTextureSampler;
            context.PixelShaderSamplers[2] = DepthMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~DofCombine()
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
