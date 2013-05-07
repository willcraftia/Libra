#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class DepthOfFieldEffect : IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.DepthOfFieldPS);
            }
        }

        #endregion

        #region Constants

        public struct Constants
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

        public float FocusRange
        {
            get { return focusRange; }
            set
            {
                focusRange = value;

                dirtyFlags |= DirtyFlags.FocusScale;
            }
        }

        public Matrix Projection
        {
            get { return projection; }
            set
            {
                projection = value;

                dirtyFlags |= DirtyFlags.InvertProjection;
            }
        }

        public float FocusDistance
        {
            get { return constants.Focus.Y; }
            set
            {
                constants.Focus.Y = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public ShaderResourceView NormalSceneMap { get; set; }

        public ShaderResourceView BluredSceneMap { get; set; }

        public ShaderResourceView DepthMap { get; set; }

        public DepthOfFieldEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<DepthOfFieldEffect, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            focusRange = 200.0f;
            projection = Matrix.Identity;

            constants.Focus.Y = 10.0f;

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

            context.PixelShaderResources[0] = NormalSceneMap;
            context.PixelShaderResources[1] = BluredSceneMap;
            context.PixelShaderResources[2] = DepthMap;

            context.PixelShaderSamplers[0] = SamplerState.LinearClamp;
            context.PixelShaderSamplers[1] = SamplerState.LinearClamp;
            context.PixelShaderSamplers[2] = SamplerState.LinearClamp;
        }

        #region IDisposable

        bool disposed;

        ~DepthOfFieldEffect()
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
