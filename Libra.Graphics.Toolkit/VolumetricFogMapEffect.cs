#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class VolumetricFogMapEffect : IEffect, IEffectMatrices, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(Resources.VolumetricFogMapVS);

                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.VolumetricFogMapPS);
            }
        }

        #endregion

        #region ParametersPerObjectVS

        public struct ParametersPerObjectVS
        {
            public Matrix WorldViewProjection;

            public Matrix WorldView;
        }

        #endregion

        #region ParametersPerObjectPS

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct ParametersPerObjectPS
        {
            public float Density;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObjectVS   = (1 << 0),
            ConstantBufferPerObjectPS   = (1 << 1),
            ViewProjection              = (1 << 2),
            WorldView                   = (1 << 3),
            WorldViewProjection         = (1 << 4)
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

        public float Density
        {
            get { return parametersPerObjectPS.Density; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObjectPS.Density = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public ShaderResourceView FrontFogDepthMap { get; set; }

        public ShaderResourceView BackFogDepthMap { get; set; }

        public SamplerState FrontFogDepthMapSampler { get; set; }

        public SamplerState BackFogDepthMapSampler { get; set; }

        public VolumetricFogMapEffect(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<VolumetricFogMapEffect, SharedDeviceResource>();

            constantBufferPerObjectVS = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObjectVS.Initialize<ParametersPerObjectVS>();
            constantBufferPerObjectPS = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObjectPS.Initialize<ParametersPerObjectPS>();

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;
            viewProjection = Matrix.Identity;

            parametersPerObjectVS.WorldViewProjection = Matrix.Identity;
            parametersPerObjectVS.WorldView = Matrix.Identity;
            parametersPerObjectPS.Density = 0.01f;

            FrontFogDepthMapSampler = SamplerState.LinearClamp;
            BackFogDepthMapSampler = SamplerState.LinearClamp;

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

            if ((dirtyFlags & DirtyFlags.WorldView) != 0)
            {
                Matrix worldView;
                Matrix.Multiply(ref world, ref view, out worldView);

                Matrix.Transpose(ref worldView, out parametersPerObjectVS.WorldView);

                dirtyFlags &= ~DirtyFlags.WorldView;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectVS;
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
                constantBufferPerObjectVS.SetData(DeviceContext, parametersPerObjectVS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObjectVS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObjectPS) != 0)
            {
                constantBufferPerObjectPS.SetData(DeviceContext, parametersPerObjectPS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObjectPS;
            }

            DeviceContext.VertexShaderConstantBuffers[0] = constantBufferPerObjectVS;
            DeviceContext.VertexShader = sharedDeviceResource.VertexShader;

            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerObjectPS;
            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;

            DeviceContext.PixelShaderResources[0] = FrontFogDepthMap;
            DeviceContext.PixelShaderResources[1] = BackFogDepthMap;
            DeviceContext.PixelShaderSamplers[0] = FrontFogDepthMapSampler;
            DeviceContext.PixelShaderSamplers[1] = BackFogDepthMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~VolumetricFogMapEffect()
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
