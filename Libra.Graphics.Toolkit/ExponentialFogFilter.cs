#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    [ViewRayRequired]
    public sealed class ExponentialFogFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.ExponentialFogFilterPS);
            }
        }

        #endregion

        #region ParametersPerScene

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        struct ParametersPerScene
        {
            [FieldOffset(0)]
            public float Density;

            [FieldOffset(16)]
            public Vector3 FogColor;

            [FieldOffset(28)]
            public float FarClipDistance;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerScene = (1 << 0)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ParametersPerScene parametersPerScene;

        ConstantBuffer constantBufferPerScene;

        DirtyFlags dirtyFlags;

        public float Density
        {
            get { return parametersPerScene.Density; }
            set
            {
                parametersPerScene.Density = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerScene;
            }
        }

        public Vector3 FogColor
        {
            get { return parametersPerScene.FogColor; }
            set
            {
                parametersPerScene.FogColor = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerScene;
            }
        }

        public float FarClipDistance
        {
            get { return parametersPerScene.FarClipDistance; }
            set
            {
                parametersPerScene.FarClipDistance = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerScene;
            }
        }

        public ShaderResourceView LinearDepthMap { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public ExponentialFogFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<ExponentialFogFilter, SharedDeviceResource>();

            constantBufferPerScene = device.CreateConstantBuffer();
            constantBufferPerScene.Initialize<ParametersPerScene>();

            parametersPerScene.Density = 0.005f;
            parametersPerScene.FogColor = Vector3.One;
            parametersPerScene.FarClipDistance = 0;

            Enabled = true;

            dirtyFlags = DirtyFlags.ConstantBufferPerScene;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.ConstantBufferPerScene) != 0)
            {
                constantBufferPerScene.SetData(context, parametersPerScene);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerScene;
            }

            context.PixelShader = sharedDeviceResource.PixelShader;
            context.PixelShaderConstantBuffers[0] = constantBufferPerScene;
            context.PixelShaderResources[0] = Texture;
            context.PixelShaderResources[1] = LinearDepthMap;
            context.PixelShaderSamplers[0] = TextureSampler;
            context.PixelShaderSamplers[1] = LinearDepthMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~ExponentialFogFilter()
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
                constantBufferPerScene.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
