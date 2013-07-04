#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class ShadowMapEffect : IEffect, IEffectMatrices, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public VertexShader VertexShader { get; private set; }

            public PixelShader DepthMapPixelShader { get; private set; }

            public PixelShader DepthVarianceMapPixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(Resources.DepthMapVS);

                DepthMapPixelShader = device.CreatePixelShader();
                DepthMapPixelShader.Initialize(Resources.DepthMapPS);

                DepthVarianceMapPixelShader = device.CreatePixelShader();
                DepthVarianceMapPixelShader.Initialize(Resources.DepthVarianceMapPS);
            }
        }

        #endregion

        #region Constants

        public struct Constants
        {
            public Matrix WorldViewProjection;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ViewProjection      = (1 << 0),
            WorldViewProjection = (1 << 1),
            Constants           = (1 << 2)
        }

        #endregion

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

        Matrix world;

        Matrix view;

        Matrix projection;

        Matrix viewProjection;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public Matrix World
        {
            get { return world; }
            set
            {
                world = value;

                dirtyFlags |= DirtyFlags.WorldViewProjection;
            }
        }

        public Matrix View
        {
            get { return view; }
            set
            {
                view = value;

                dirtyFlags |= DirtyFlags.ViewProjection;
            }
        }

        public Matrix Projection
        {
            get { return projection; }
            set
            {
                projection = value;

                dirtyFlags |= DirtyFlags.ViewProjection;
            }
        }

        public ShadowMapForm Form { get; set; }

        public ShadowMapEffect(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<ShadowMapEffect, SharedDeviceResource>();

            constantBuffer = deviceContext.Device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;
            viewProjection = Matrix.Identity;
            constants.WorldViewProjection = Matrix.Identity;

            Form = ShadowMapForm.Basic;

            dirtyFlags = DirtyFlags.Constants;
        }

        public void Apply()
        {
            if ((dirtyFlags & DirtyFlags.ViewProjection) != 0)
            {
                Matrix.Multiply(ref view, ref projection, out viewProjection);

                dirtyFlags &= ~DirtyFlags.ViewProjection;
                dirtyFlags |= DirtyFlags.WorldViewProjection;
            }

            if ((dirtyFlags & DirtyFlags.WorldViewProjection) != 0)
            {
                Matrix worldViewProjection;
                Matrix.Multiply(ref world, ref viewProjection, out worldViewProjection);

                Matrix.Transpose(ref worldViewProjection, out constants.WorldViewProjection);

                dirtyFlags &= ~DirtyFlags.WorldViewProjection;
                dirtyFlags |= DirtyFlags.Constants;
            }

            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                DeviceContext.SetData(constantBuffer, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            DeviceContext.VertexShaderConstantBuffers[0] = constantBuffer;
            DeviceContext.VertexShader = sharedDeviceResource.VertexShader;

            if (Form == ShadowMapForm.Variance)
            {
                DeviceContext.PixelShader = sharedDeviceResource.DepthVarianceMapPixelShader;
            }
            else
            {
                DeviceContext.PixelShader = sharedDeviceResource.DepthMapPixelShader;
            }
        }

        #region IDisposable

        bool disposed;

        ~ShadowMapEffect()
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
