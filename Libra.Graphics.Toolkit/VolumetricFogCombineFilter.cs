#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class VolumetricFogCombineFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.VolumetricFogCombineFilterPS);
            }
        }

        #endregion

        #region ParametersPerShader

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        struct ParametersPerShader
        {
            public Vector3 FogColor;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerShader = (1 << 0)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerShader;

        ParametersPerShader parametersPerShader;

        DirtyFlags dirtyFlags;

        public Vector3 FogColor
        {
            get { return parametersPerShader.FogColor; }
            set
            {
                parametersPerShader.FogColor = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerShader;
            }
        }

        public ShaderResourceView VolumetricFogMap { get; set; }

        public SamplerState VolumetricFogMapSampler { get; set; }

        public bool Enabled { get; set; }

        public VolumetricFogCombineFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<VolumetricFogCombineFilter, SharedDeviceResource>();

            constantBufferPerShader = device.CreateConstantBuffer();
            constantBufferPerShader.Initialize<ParametersPerShader>();

            parametersPerShader.FogColor = Vector3.One;

            VolumetricFogMapSampler = SamplerState.LinearClamp;

            Enabled = true;

            dirtyFlags = DirtyFlags.ConstantBufferPerShader;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.ConstantBufferPerShader) != 0)
            {
                constantBufferPerShader.SetData(context, parametersPerShader);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerShader;
            }

            context.PixelShaderConstantBuffers[0] = constantBufferPerShader;
            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[1] = VolumetricFogMap;
            context.PixelShaderSamplers[1] = VolumetricFogMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~VolumetricFogCombineFilter()
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
                constantBufferPerShader.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
