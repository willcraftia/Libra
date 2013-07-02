#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class LinearFogDepthMapEffect : IEffect, IEffectMatrices, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(Resources.LinearFogDepthMapVS);

                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.LinearFogDepthMapPS);
            }
        }

        #endregion

        #region ParametersPerObject

        public struct ParametersPerObject
        {
            public Matrix WorldViewProjection;

            public Matrix WorldView;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObject = (1 << 0),
            ViewProjection          = (1 << 1),
            WorldView               = (1 << 2),
            WorldViewProjection     = (1 << 3)
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

                dirtyFlags |= DirtyFlags.WorldView | DirtyFlags.WorldViewProjection;
            }
        }

        public Matrix View
        {
            get { return view; }
            set
            {
                view = value;

                dirtyFlags |= DirtyFlags.WorldView | DirtyFlags.ViewProjection;
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

        public ShaderResourceView LinearDepthMap { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public LinearFogDepthMapEffect(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<LinearFogDepthMapEffect, SharedDeviceResource>();

            constantBufferPerObject = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObject.Initialize<ParametersPerObject>();

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;
            viewProjection = Matrix.Identity;
            parametersPerObject.WorldViewProjection = Matrix.Identity;
            parametersPerObject.WorldView = Matrix.Identity;

            LinearDepthMapSampler = SamplerState.LinearClamp;

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

            if ((dirtyFlags & DirtyFlags.WorldView) != 0)
            {
                Matrix worldView;
                Matrix.Multiply(ref world, ref view, out worldView);

                Matrix.Transpose(ref worldView, out parametersPerObject.WorldView);

                dirtyFlags &= ~DirtyFlags.WorldView;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
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
                constantBufferPerObject.SetData(DeviceContext, parametersPerObject);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObject;
            }

            DeviceContext.VertexShaderConstantBuffers[0] = constantBufferPerObject;
            DeviceContext.VertexShader = sharedDeviceResource.VertexShader;
            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;

            DeviceContext.PixelShaderResources[0] = LinearDepthMap;
            DeviceContext.PixelShaderSamplers[0] = LinearDepthMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~LinearFogDepthMapEffect()
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
