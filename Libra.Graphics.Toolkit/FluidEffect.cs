#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class FluidEffect : IEffect, IEffectMatrices, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(Resources.FluidVS);

                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.FluidPS);
            }
        }

        #endregion

        #region ParametersPerObjectVS

        struct ParametersPerObjectVS
        {
            public Matrix World;

            public Matrix WorldViewProjection;

            public Matrix WorldReflectionProjection;
        }

        #endregion

        #region ParametersPerCameraPS

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        struct ParametersPerCameraPS
        {
            public Vector3 EyePosition;
        }

        #endregion

        #region ParametersPerObjectPS

        [StructLayout(LayoutKind.Explicit, Size = 48)]
        struct ParametersPerObjectPS
        {
            [FieldOffset(0)]
            public float RippleScale;

            [FieldOffset(4)]
            public Vector3 FluidColor;

            [FieldOffset(16)]
            public Vector3 FluidDeepColor;

            [FieldOffset(28)]
            public float FluidDeepColorDistance;

            [FieldOffset(32)]
            public bool FluidColorEnabled;

            [FieldOffset(36)]
            public bool FluidDeepColorEnabled;

            [FieldOffset(40)]
            public float ReflectionCoeff;
        }

        #endregion

        #region ParametersPerFramePS

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        struct ParametersPerFramePS
        {
            public Vector2 Offset;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObjectVS   = (1 << 0),
            ConstantBufferPerCameraPS   = (1 << 1),
            ConstantBufferPerObjectPS   = (1 << 2),
            ConstantBufferPerFramePS    = (1 << 3),
            EyePosition                 = (1 << 4),
            World                       = (1 << 5),
            ViewProjection              = (1 << 6),
            WorldViewProjection         = (1 << 7),
            WorldReflectionProjection   = (1 << 8),
            ReflectionCoeff             = (1 << 9)
        }

        #endregion

        // 0℃ 1、気圧。
        public const float RefractiveIndexAir = 1.000292f;

        // 20℃
        public const float RefracticeIndexWater = 1.3334f;

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerObjectVS;

        ConstantBuffer constantBufferPerCameraPS;

        ConstantBuffer constantBufferPerObjectPS;

        ConstantBuffer constantBufferPerFramePS;

        ParametersPerObjectVS parametersPerObjectVS;

        ParametersPerCameraPS parametersPerCameraPS;

        ParametersPerObjectPS parametersPerObjectPS;

        ParametersPerFramePS parametersPerFramePS;

        Matrix world;

        Matrix view;

        Matrix projection;

        Matrix reflectionView;
        
        Matrix viewProjection;

        float refractiveIndex1;

        float refractiveIndex2;

        DirtyFlags dirtyFlags;

        public Matrix World
        {
            get { return world; }
            set
            {
                world = value;

                dirtyFlags |=
                    DirtyFlags.World |
                    DirtyFlags.WorldViewProjection;
            }
        }

        public Matrix View
        {
            get { return view; }
            set
            {
                view = value;

                dirtyFlags |=
                    DirtyFlags.EyePosition |
                    DirtyFlags.ViewProjection |
                    DirtyFlags.WorldReflectionProjection;
            }
        }

        public Matrix Projection
        {
            get { return projection; }
            set
            {
                projection = value;

                dirtyFlags |=
                    DirtyFlags.ViewProjection |
                    DirtyFlags.WorldReflectionProjection;
            }
        }

        public Matrix ReflectionView
        {
            get { return reflectionView; }
            set
            {
                reflectionView = value;

                dirtyFlags |= DirtyFlags.WorldReflectionProjection;
            }
        }

        public float RippleScale
        {
            get { return parametersPerObjectPS.RippleScale; }
            set
            {
                parametersPerObjectPS.RippleScale = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public Vector3 FluidColor
        {
            get { return parametersPerObjectPS.FluidColor; }
            set
            {
                parametersPerObjectPS.FluidColor = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public Vector3 FluidDeepColor
        {
            get { return parametersPerObjectPS.FluidDeepColor; }
            set
            {
                parametersPerObjectPS.FluidDeepColor = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public float FluidDeepColorDistance
        {
            get { return parametersPerObjectPS.FluidDeepColorDistance; }
            set
            {
                parametersPerObjectPS.FluidDeepColorDistance = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public bool FluidColorEnabled
        {
            get { return parametersPerObjectPS.FluidColorEnabled; }
            set
            {
                parametersPerObjectPS.FluidColorEnabled = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public bool FluidDeepColorEnabled
        {
            get { return parametersPerObjectPS.FluidDeepColorEnabled; }
            set
            {
                parametersPerObjectPS.FluidDeepColorEnabled = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public Vector2 Offset
        {
            get { return parametersPerFramePS.Offset; }
            set
            {
                parametersPerFramePS.Offset = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerFramePS;
            }
        }

        public float RefractiveIndex1
        {
            get { return refractiveIndex1; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                refractiveIndex1 = value;

                dirtyFlags |= DirtyFlags.ReflectionCoeff;
            }
        }

        public float RefractiveIndex2
        {
            get { return refractiveIndex2; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                refractiveIndex2 = value;

                dirtyFlags |= DirtyFlags.ReflectionCoeff;
            }
        }

        public ShaderResourceView NormalMap { get; set; }

        public ShaderResourceView ReflectionMap { get; set; }

        public ShaderResourceView RefractionMap { get; set; }

        public SamplerState NormalMapSampler { get; set; }

        public SamplerState ReflectionMapSampler { get; set; }

        public SamplerState RefractionMapSampler { get; set; }

        public FluidEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<FluidEffect, SharedDeviceResource>();

            constantBufferPerObjectVS = device.CreateConstantBuffer();
            constantBufferPerObjectVS.Initialize<ParametersPerObjectVS>();
            constantBufferPerCameraPS = device.CreateConstantBuffer();
            constantBufferPerCameraPS.Initialize<ParametersPerCameraPS>();
            constantBufferPerObjectPS = device.CreateConstantBuffer();
            constantBufferPerObjectPS.Initialize<ParametersPerObjectPS>();
            constantBufferPerFramePS = device.CreateConstantBuffer();
            constantBufferPerFramePS.Initialize<ParametersPerFramePS>();

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;
            reflectionView = Matrix.Identity;
            viewProjection = Matrix.Identity;
            refractiveIndex1 = RefractiveIndexAir;
            refractiveIndex2 = RefracticeIndexWater;

            parametersPerCameraPS.EyePosition = Vector3.Zero;
            parametersPerObjectVS.WorldViewProjection = Matrix.Identity;
            parametersPerObjectVS.WorldReflectionProjection = Matrix.Identity;
            parametersPerObjectPS.FluidColor = new Vector3(0.0f, 0.55f, 0.515f);
            parametersPerObjectPS.FluidDeepColor = new Vector3(0.0f, 0.15f, 0.115f);
            parametersPerObjectPS.FluidDeepColorDistance = 50.0f;
            parametersPerObjectPS.RippleScale = 0.01f;
            parametersPerFramePS.Offset = Vector2.Zero;

            NormalMapSampler = SamplerState.LinearWrap;
            ReflectionMapSampler = SamplerState.LinearClamp;
            RefractionMapSampler = SamplerState.LinearClamp;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerObjectVS |
                DirtyFlags.ConstantBufferPerCameraPS |
                DirtyFlags.ConstantBufferPerObjectPS |
                DirtyFlags.ConstantBufferPerFramePS |
                DirtyFlags.ReflectionCoeff;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.EyePosition) != 0)
            {
                Matrix inverseView;
                Matrix.Invert(ref view, out inverseView);

                parametersPerCameraPS.EyePosition = inverseView.Translation;

                dirtyFlags &= ~DirtyFlags.EyePosition;
                dirtyFlags |= DirtyFlags.ConstantBufferPerCameraPS;
            }

            if ((dirtyFlags & DirtyFlags.World) != 0)
            {
                Matrix.Transpose(ref world, out parametersPerObjectVS.World);

                dirtyFlags &= ~DirtyFlags.World;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectVS;
            }

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

            if ((dirtyFlags & DirtyFlags.WorldReflectionProjection) != 0)
            {
                // 反射仮想カメラのビュー×射影行列。
                Matrix reflectionProjection;
                Matrix.Multiply(ref reflectionView, ref projection, out reflectionProjection);

                // 反射仮想カメラのワールド×ビュー×射影行列。
                Matrix worldReflectionProjection;
                Matrix.Multiply(ref world, ref reflectionProjection, out worldReflectionProjection);

                // 転置して設定。
                Matrix.Transpose(ref worldReflectionProjection, out parametersPerObjectVS.WorldReflectionProjection);

                dirtyFlags &= ~DirtyFlags.WorldReflectionProjection;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectVS;
            }

            if ((dirtyFlags & DirtyFlags.ReflectionCoeff) != 0)
            {
                float r = (refractiveIndex1 - refractiveIndex2) / (refractiveIndex1 + refractiveIndex2);
                r *= r;

                parametersPerObjectPS.ReflectionCoeff = r;

                dirtyFlags &= ~DirtyFlags.ReflectionCoeff;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObjectVS) != 0)
            {
                constantBufferPerObjectVS.SetData(context, parametersPerObjectVS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObjectVS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerCameraPS) != 0)
            {
                constantBufferPerCameraPS.SetData(context, parametersPerCameraPS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerCameraPS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObjectPS) != 0)
            {
                constantBufferPerObjectPS.SetData(context, parametersPerObjectPS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObjectPS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerFramePS) != 0)
            {
                constantBufferPerFramePS.SetData(context, parametersPerFramePS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerFramePS;
            }

            context.VertexShader = sharedDeviceResource.VertexShader;
            context.VertexShaderConstantBuffers[0] = constantBufferPerObjectVS;

            context.PixelShader = sharedDeviceResource.PixelShader;
            context.PixelShaderConstantBuffers[0] = constantBufferPerCameraPS;
            context.PixelShaderConstantBuffers[1] = constantBufferPerObjectPS;
            context.PixelShaderConstantBuffers[2] = constantBufferPerFramePS;
            context.PixelShaderResources[0] = NormalMap;
            context.PixelShaderResources[1] = ReflectionMap;
            context.PixelShaderResources[2] = RefractionMap;
            context.PixelShaderSamplers[0] = NormalMapSampler;
            context.PixelShaderSamplers[1] = ReflectionMapSampler;
            context.PixelShaderSamplers[2] = RefractionMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~FluidEffect()
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
                constantBufferPerObjectPS.Dispose();
                constantBufferPerFramePS.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
