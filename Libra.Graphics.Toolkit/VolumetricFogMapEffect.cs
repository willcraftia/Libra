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

        #region ConstantsVS

        public struct ConstantsVS
        {
            public Matrix WorldViewProjection;

            public Matrix WorldView;
        }

        #endregion

        #region ConstantsPS

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct ConstantsPS
        {
            public float Density;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ViewProjection      = (1 << 0),
            WorldView           = (1 << 1),
            WorldViewProjection = (1 << 2),
            ConstantsVS         = (1 << 3),
            ConstantsPS         = (1 << 4)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferVS;

        ConstantBuffer constantBufferPS;

        ConstantsVS constantsVS;

        ConstantsPS constantsPS;

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
            get { return constantsPS.Density; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constantsPS.Density = value;

                dirtyFlags |= DirtyFlags.ConstantsPS;
            }
        }

        public ShaderResourceView FrontFogDepthMap { get; set; }

        public ShaderResourceView BackFogDepthMap { get; set; }

        public SamplerState FrontFogDepthMapSampler { get; set; }

        public SamplerState BackFogDepthMapSampler { get; set; }

        public VolumetricFogMapEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<VolumetricFogMapEffect, SharedDeviceResource>();

            constantBufferVS = device.CreateConstantBuffer();
            constantBufferVS.Initialize<ConstantsVS>();
            constantBufferPS = device.CreateConstantBuffer();
            constantBufferPS.Initialize<ConstantsPS>();

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;
            viewProjection = Matrix.Identity;

            constantsVS.WorldViewProjection = Matrix.Identity;
            constantsVS.WorldView = Matrix.Identity;
            constantsPS.Density = 0.01f;

            FrontFogDepthMapSampler = SamplerState.LinearClamp;
            BackFogDepthMapSampler = SamplerState.LinearClamp;

            dirtyFlags = DirtyFlags.ConstantsVS | DirtyFlags.ConstantsPS;
        }

        public void Apply(DeviceContext context)
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

                Matrix.Transpose(ref worldView, out constantsVS.WorldView);

                dirtyFlags &= ~DirtyFlags.WorldView;
                dirtyFlags |= DirtyFlags.ConstantsVS;
            }

            if ((dirtyFlags & DirtyFlags.WorldViewProjection) != 0)
            {
                Matrix worldViewProjection;
                Matrix.Multiply(ref world, ref viewProjection, out worldViewProjection);

                Matrix.Transpose(ref worldViewProjection, out constantsVS.WorldViewProjection);

                dirtyFlags &= ~DirtyFlags.WorldViewProjection;
                dirtyFlags |= DirtyFlags.ConstantsVS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantsVS) != 0)
            {
                constantBufferVS.SetData(context, constantsVS);

                dirtyFlags &= ~DirtyFlags.ConstantsVS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantsPS) != 0)
            {
                constantBufferPS.SetData(context, constantsPS);

                dirtyFlags &= ~DirtyFlags.ConstantsPS;
            }

            context.VertexShaderConstantBuffers[0] = constantBufferVS;
            context.VertexShader = sharedDeviceResource.VertexShader;

            context.PixelShaderConstantBuffers[0] = constantBufferPS;
            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[0] = FrontFogDepthMap;
            context.PixelShaderResources[1] = BackFogDepthMap;
            context.PixelShaderSamplers[0] = FrontFogDepthMapSampler;
            context.PixelShaderSamplers[1] = BackFogDepthMapSampler;
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
                constantBufferVS.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
