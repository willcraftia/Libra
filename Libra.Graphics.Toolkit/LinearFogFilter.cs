#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    [ViewRayRequired]
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
            // fog = (end - distance) / (end - start)
            //     = end / (end - start) - distance / (end - start)
            //     = intercept + distance * gradient

            [FieldOffset(0)]
            public float FogGradient;

            [FieldOffset(4)]
            public float FogIntercept;

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
            ConstantBufferPerScene  = (1 << 0),
            FogGradientIntercept    = (1 << 1)
        }

        #endregion

        SharedDeviceResource sharedDeviceResource;

        ParametersPerScene parametersPerScene;

        ConstantBuffer constantBufferPerScene;

        float fogStart;

        float fogEnd;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public float FogStart
        {
            get { return fogStart; }
            set
            {
                fogStart = value;

                dirtyFlags |= DirtyFlags.FogGradientIntercept;
            }
        }

        public float FogEnd
        {
            get { return fogEnd; }
            set
            {
                fogEnd = value;

                dirtyFlags |= DirtyFlags.FogGradientIntercept;
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

        public LinearFogFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<LinearFogFilter, SharedDeviceResource>();

            constantBufferPerScene = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerScene.Initialize<ParametersPerScene>();

            fogStart = 0.0f;
            fogEnd = 0.0f;

            parametersPerScene.FogColor = Vector3.One;
            parametersPerScene.FarClipDistance = 0;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerScene |
                DirtyFlags.FogGradientIntercept;
        }

        public void Apply()
        {
            if ((dirtyFlags & DirtyFlags.FogGradientIntercept) != 0)
            {
                float distance = fogEnd - fogStart;

                parametersPerScene.FogGradient = -1.0f / distance;
                parametersPerScene.FogIntercept = fogEnd / distance;

                dirtyFlags &= ~DirtyFlags.FogGradientIntercept;
                dirtyFlags |= DirtyFlags.ConstantBufferPerScene;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerScene) != 0)
            {
                DeviceContext.SetData(constantBufferPerScene, parametersPerScene);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerScene;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerScene;
            DeviceContext.PixelShaderResources[0] = Texture;
            DeviceContext.PixelShaderResources[1] = LinearDepthMap;
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
            DeviceContext.PixelShaderSamplers[1] = LinearDepthMapSampler;
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
