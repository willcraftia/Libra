﻿#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class LinearFogFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.LinearFogFilterPS);
            }
        }

        #endregion

        #region ParametersPerScene

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        struct ParametersPerScene
        {
            [FieldOffset(0)]
            public float FogRatio;

            [FieldOffset(4)]
            public float FogOffset;

            [FieldOffset(16)]
            public Vector3 FogColor;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerScene  = (1 << 0),
            FogRatioOffset          = (1 << 1)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ParametersPerScene parametersPerScene;

        ConstantBuffer constantBufferPerScene;

        float fogStart;

        float fogEnd;

        DirtyFlags dirtyFlags;

        public float FogStart
        {
            get { return fogStart; }
            set
            {
                fogStart = value;

                dirtyFlags |= DirtyFlags.FogRatioOffset;
            }
        }

        public float FogEnd
        {
            get { return fogEnd; }
            set
            {
                fogEnd = value;

                dirtyFlags |= DirtyFlags.FogRatioOffset;
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

        public ShaderResourceView LinearDepthMap { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public LinearFogFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<LinearFogFilter, SharedDeviceResource>();

            constantBufferPerScene = device.CreateConstantBuffer();
            constantBufferPerScene.Initialize<ParametersPerScene>();

            fogStart = 0.0f;
            fogEnd = 0.0f;

            parametersPerScene.FogColor = Vector3.One;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerScene |
                DirtyFlags.FogRatioOffset;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.FogRatioOffset) != 0)
            {
                float distance = fogEnd - fogStart;

                parametersPerScene.FogRatio = 1.0f / distance;
                parametersPerScene.FogOffset = -fogStart / distance;

                dirtyFlags &= ~DirtyFlags.FogRatioOffset;
                dirtyFlags |= DirtyFlags.ConstantBufferPerScene;
            }

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

        ~LinearFogFilter()
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
