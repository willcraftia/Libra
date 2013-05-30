#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    [ViewRayRequired]
    public sealed class HeightFogFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.HeightFogFilterPS);
            }
        }

        #endregion

        #region ParametersPerScene

        [StructLayout(LayoutKind.Explicit, Size = 96)]
        struct ParametersPerScene
        {
            // fog = (height - min) / (max - min)
            //     = height / (max - min) - min / (max - min)
            //     = height * gradient + intercept

            [FieldOffset(0)]
            public float FogGradient;

            [FieldOffset(4)]
            public float FogIntercept;

            [FieldOffset(16)]
            public Vector3 FogColor;

            [FieldOffset(32)]
            public Matrix InverseView;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerScene  = (1 << 0),
            FogGradientIntercept    = (1 << 1),
            InverseView             = (1 << 2)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ParametersPerScene parametersPerScene;

        ConstantBuffer constantBufferPerScene;

        float fogMinHeight;

        float fogMaxHeight;

        Matrix view;

        DirtyFlags dirtyFlags;

        public float FogMinHeight
        {
            get { return fogMinHeight; }
            set
            {
                fogMinHeight = value;

                dirtyFlags |= DirtyFlags.FogGradientIntercept;
            }
        }

        public float FogMaxHeight
        {
            get { return fogMaxHeight; }
            set
            {
                fogMaxHeight = value;

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

        public Matrix View
        {
            get { return view; }
            set
            {
                view = value;

                dirtyFlags |= DirtyFlags.InverseView;
            }
        }

        public ShaderResourceView LinearDepthMap { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public HeightFogFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<HeightFogFilter, SharedDeviceResource>();

            constantBufferPerScene = device.CreateConstantBuffer();
            constantBufferPerScene.Initialize<ParametersPerScene>();

            fogMinHeight = 0.0f;
            fogMaxHeight = 0.0f;
            view = Matrix.Identity;

            parametersPerScene.FogColor = Vector3.One;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerScene |
                DirtyFlags.FogGradientIntercept |
                DirtyFlags.InverseView;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.FogGradientIntercept) != 0)
            {
                float distance = fogMaxHeight - fogMinHeight;

                parametersPerScene.FogGradient = 1.0f / distance;
                parametersPerScene.FogIntercept = -fogMinHeight / distance;

                dirtyFlags &= ~DirtyFlags.FogGradientIntercept;
                dirtyFlags |= DirtyFlags.ConstantBufferPerScene;
            }

            if ((dirtyFlags & DirtyFlags.InverseView) != 0)
            {
                Matrix inverseView;
                Matrix.Invert(ref view, out inverseView);

                Matrix.Transpose(ref inverseView, out parametersPerScene.InverseView);

                dirtyFlags &= ~DirtyFlags.InverseView;
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

        ~HeightFogFilter()
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
