#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class DepthMapEffect : IEffect, IEffectMatrices, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(Resources.DepthMapVS);

                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.DepthMapPS);
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

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

        Matrix world;

        Matrix view;

        Matrix projection;

        Matrix viewProjection;

        DirtyFlags dirtyFlags;

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

        public DepthMapEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<ShadowMapEffect, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;

            dirtyFlags = DirtyFlags.WorldViewProjection | DirtyFlags.ViewProjection;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.ViewProjection) != 0)
            {
                Matrix.Multiply(ref view, ref projection, out viewProjection);

                dirtyFlags &= ~DirtyFlags.ViewProjection;
                dirtyFlags |= DirtyFlags.Constants;
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
                constantBuffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            context.VertexShaderConstantBuffers[0] = constantBuffer;
            context.VertexShader = sharedDeviceResource.VertexShader;
            context.PixelShader = sharedDeviceResource.PixelShader;
        }

        #region IDisposable

        bool disposed;

        ~DepthMapEffect()
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
