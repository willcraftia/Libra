#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class SingleColorObjectEffect : IEffect, IEffectMatrices, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(Resources.SingleColorObjectVS);

                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.SingleColorObjectPS);
            }
        }

        #endregion

        #region ParametersPerObjectVS

        public struct ParametersPerObjectVS
        {
            public Matrix WorldViewProjection;
        }

        #endregion

        #region ParametersPerObjectPS

        public struct ParametersPerObjectPS
        {
            public Vector4 Color;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObjectVS   = (1 << 0),
            ConstantBufferPerObjectPS   = (1 << 1),
            ViewProjection              = (1 << 2),
            WorldViewProjection         = (1 << 3)
        }

        #endregion

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerObjectVS;

        ConstantBuffer constantBufferPerObjectPS;

        ParametersPerObjectVS parametersPerObjectVS;

        ParametersPerObjectPS parametersPerObjectPS;

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

        public Vector4 Color
        {
            get { return parametersPerObjectPS.Color; }
            set
            {
                parametersPerObjectPS.Color = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public SingleColorObjectEffect(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<SingleColorObjectEffect, SharedDeviceResource>();

            constantBufferPerObjectVS = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObjectVS.Initialize<ParametersPerObjectVS>();

            constantBufferPerObjectPS = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObjectPS.Initialize<ParametersPerObjectPS>();

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;
            viewProjection = Matrix.Identity;

            parametersPerObjectVS.WorldViewProjection = Matrix.Identity;
            parametersPerObjectPS.Color = Vector4.Zero;

            dirtyFlags = DirtyFlags.ConstantBufferPerObjectVS | DirtyFlags.ConstantBufferPerObjectPS;
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

                Matrix.Transpose(ref worldViewProjection, out parametersPerObjectVS.WorldViewProjection);

                dirtyFlags &= ~DirtyFlags.WorldViewProjection;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectVS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObjectVS) != 0)
            {
                DeviceContext.SetData(constantBufferPerObjectVS, parametersPerObjectVS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObjectVS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObjectPS) != 0)
            {
                DeviceContext.SetData(constantBufferPerObjectPS, parametersPerObjectPS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObjectPS;
            }

            DeviceContext.VertexShaderConstantBuffers[0] = constantBufferPerObjectVS;
            DeviceContext.VertexShader = sharedDeviceResource.VertexShader;

            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerObjectPS;
            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
        }

        #region IDisposable

        bool disposed;

        ~SingleColorObjectEffect()
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
                constantBufferPerObjectVS.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
