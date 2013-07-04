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

        #region ParametersPerObject

        public struct ParametersPerObject
        {
            public Matrix WorldViewProjection;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObject = (1 << 0),
            ViewProjection          = (1 << 1),
            WorldViewProjection     = (1 << 2)
        }

        #endregion

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerObject;

        ParametersPerObject parametersPerObject;

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

        public DepthMapEffect(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<DepthMapEffect, SharedDeviceResource>();

            constantBufferPerObject = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObject.Initialize<ParametersPerObject>();

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;
            viewProjection = Matrix.Identity;
            parametersPerObject.WorldViewProjection = Matrix.Identity;

            dirtyFlags = DirtyFlags.ConstantBufferPerObject;
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

                Matrix.Transpose(ref worldViewProjection, out parametersPerObject.WorldViewProjection);

                dirtyFlags &= ~DirtyFlags.WorldViewProjection;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObject) != 0)
            {
                DeviceContext.SetData(constantBufferPerObject, parametersPerObject);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObject;
            }

            DeviceContext.VertexShaderConstantBuffers[0] = constantBufferPerObject;
            DeviceContext.VertexShader = sharedDeviceResource.VertexShader;
            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
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
                constantBufferPerObject.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
