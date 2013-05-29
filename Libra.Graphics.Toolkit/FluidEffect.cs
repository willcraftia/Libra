﻿#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    // メモ
    //
    // 反射マップや屈折マップを準備することが面倒な場合には、
    // 適当な単色テクスチャを指定する。

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
            public Matrix WorldView;

            public Matrix WorldViewProjection;

            public Matrix WorldReflectionProjection;
        }

        #endregion

        #region ParametersPerObjectPS

        [StructLayout(LayoutKind.Explicit, Size = 144)]
        struct ParametersPerObjectPS
        {
            [FieldOffset(0)]
            public Vector4 DiffuseColor;

            [FieldOffset(16)]
            public Vector3 EmissiveColor;

            [FieldOffset(32)]
            public Vector3 SpecularColor;

            [FieldOffset(44)]
            public float SpecularPower;

            [FieldOffset(48)]
            public float RippleScale;

            [FieldOffset(52)]
            public float RefractionAttenuation;

            [FieldOffset(56)]
            public float ReflectionCoeff;

            [FieldOffset(64)]
            public Matrix WorldView;
        }

        #endregion

        #region ParametersPerFramePS

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        struct ParametersPerFramePS
        {
            [FieldOffset(0)]
            public Vector2 Offset0;

            [FieldOffset(8)]
            public Vector2 Offset1;

            [FieldOffset(16)]
            public Vector3 LightDirection;
        }

        #endregion

        #region ParametersPerSceneVS

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        struct ParametersPerSceneVS
        {
            [FieldOffset(0)]
            public bool FogEnabled;

            [FieldOffset(4)]
            public float FogStart;

            [FieldOffset(8)]
            public float FogEnd;
        }

        #endregion

        #region ParametersPerScenePS

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        struct ParametersPerScenePS
        {
            public Vector3 FogColor;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObjectVS   = (1 << 0),
            ConstantBufferPerObjectPS   = (1 << 1),
            ConstantBufferPerFramePS    = (1 << 2),
            ConstantBufferPerSceneVS    = (1 << 3),
            ConstantBufferPerScenePS    = (1 << 4),
            EyePosition                 = (1 << 5),
            WorldView                   = (1 << 6),
            ViewProjection              = (1 << 7),
            WorldViewProjection         = (1 << 8),
            WorldReflectionProjection   = (1 << 9),
            ReflectionCoeff             = (1 << 10),
            MaterialColor               = (1 << 11)
        }

        #endregion

        // 0℃ 1、気圧。
        public const float RefractiveIndexAir = 1.000292f;

        // 20℃
        public const float RefracticeIndexWater = 1.3334f;

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerObjectVS;

        ConstantBuffer constantBufferPerObjectPS;

        ConstantBuffer constantBufferPerFramePS;

        ConstantBuffer constantBufferPerSceneVS;

        ConstantBuffer constantBufferPerScenePS;

        ParametersPerObjectVS parametersPerObjectVS;

        ParametersPerObjectPS parametersPerObjectPS;

        ParametersPerFramePS parametersPerFramePS;

        ParametersPerSceneVS parametersPerSceneVS;

        ParametersPerScenePS parametersPerScenePS;

        Matrix world;

        Matrix view;

        Matrix projection;

        Matrix worldView;

        Matrix reflectionView;
        
        Matrix viewProjection;

        float refractiveIndex1;

        float refractiveIndex2;

        Vector3 ambientLightColor;

        Vector3 diffuseColor;

        Vector3 emissiveColor;

        float alpha;

        DirtyFlags dirtyFlags;

        public Matrix World
        {
            get { return world; }
            set
            {
                world = value;

                dirtyFlags |=
                    DirtyFlags.WorldView |
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
                    DirtyFlags.WorldView |
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

        public Vector3 AmbientLightColor
        {
            get { return ambientLightColor; }
            set
            {
                ambientLightColor = value;

                dirtyFlags |= DirtyFlags.MaterialColor;
            }
        }

        public float Alpha
        {
            get { return alpha; }
            set
            {
                alpha = value;

                dirtyFlags |= DirtyFlags.MaterialColor;
            }
        }

        public Vector3 DiffuseColor
        {
            get { return diffuseColor; }
            set
            {
                diffuseColor = value;

                dirtyFlags |= DirtyFlags.MaterialColor;
            }
        }

        public Vector3 EmissiveColor
        {
            get { return emissiveColor; }
            set
            {
                emissiveColor = value;

                dirtyFlags |= DirtyFlags.MaterialColor;
            }
        }

        public Vector3 SpecularColor
        {
            get { return parametersPerObjectPS.SpecularColor; }
            set
            {
                parametersPerObjectPS.SpecularColor = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public float SpecularPower
        {
            get { return parametersPerObjectPS.SpecularPower; }
            set
            {
                parametersPerObjectPS.SpecularPower = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public float RefractionAttenuation
        {
            get { return parametersPerObjectPS.RefractionAttenuation; }
            set
            {
                parametersPerObjectPS.RefractionAttenuation = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
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

        public Vector2 Offset0
        {
            get { return parametersPerFramePS.Offset0; }
            set
            {
                parametersPerFramePS.Offset0 = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerFramePS;
            }
        }

        public Vector2 Offset1
        {
            get { return parametersPerFramePS.Offset1; }
            set
            {
                parametersPerFramePS.Offset1 = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerFramePS;
            }
        }

        public Vector3 LightDirection
        {
            get { return parametersPerFramePS.LightDirection; }
            set
            {
                parametersPerFramePS.LightDirection = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerFramePS;
            }
        }

        public bool FogEnabled
        {
            get { return parametersPerSceneVS.FogEnabled; }
            set
            {
                parametersPerSceneVS.FogEnabled = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerSceneVS;
            }
        }

        public float FogStart
        {
            get { return parametersPerSceneVS.FogStart; }
            set
            {
                parametersPerSceneVS.FogStart = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerSceneVS;
            }
        }

        public float FogEnd
        {
            get { return parametersPerSceneVS.FogEnd; }
            set
            {
                parametersPerSceneVS.FogEnd = value;
                
                dirtyFlags |= DirtyFlags.ConstantBufferPerSceneVS;
            }
        }

        public Vector3 FogColor
        {
            get { return parametersPerScenePS.FogColor; }
            set
            {
                parametersPerScenePS.FogColor = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerScenePS;
            }
        }

        public ShaderResourceView NormalMap0 { get; set; }

        public ShaderResourceView NormalMap1 { get; set; }

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
            constantBufferPerObjectPS = device.CreateConstantBuffer();
            constantBufferPerObjectPS.Initialize<ParametersPerObjectPS>();
            constantBufferPerFramePS = device.CreateConstantBuffer();
            constantBufferPerFramePS.Initialize<ParametersPerFramePS>();
            constantBufferPerSceneVS = device.CreateConstantBuffer();
            constantBufferPerSceneVS.Initialize<ParametersPerSceneVS>();
            constantBufferPerScenePS = device.CreateConstantBuffer();
            constantBufferPerScenePS.Initialize<ParametersPerScenePS>();

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;
            reflectionView = Matrix.Identity;
            viewProjection = Matrix.Identity;
            refractiveIndex1 = RefractiveIndexAir;
            refractiveIndex2 = RefracticeIndexWater;
            diffuseColor = new Vector3(0.0f, 0.55f, 0.515f);
            alpha = 1.0f;
            
            parametersPerObjectVS.WorldViewProjection = Matrix.Identity;
            parametersPerObjectVS.WorldReflectionProjection = Matrix.Identity;

            parametersPerObjectPS.EmissiveColor = Vector3.Zero;
            parametersPerObjectPS.SpecularColor = Vector3.One;
            parametersPerObjectPS.SpecularPower = 16;
            parametersPerObjectPS.RefractionAttenuation = 50.0f;
            parametersPerObjectPS.RippleScale = 0.1f;

            parametersPerFramePS.Offset0 = Vector2.Zero;
            parametersPerFramePS.LightDirection = Vector3.Down;

            parametersPerSceneVS.FogEnabled = false;
            parametersPerSceneVS.FogStart = 0.0f;
            parametersPerSceneVS.FogEnd = 0.0f;

            parametersPerScenePS.FogColor = Vector3.One;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerObjectVS |
                DirtyFlags.ConstantBufferPerObjectPS |
                DirtyFlags.ConstantBufferPerFramePS |
                DirtyFlags.ReflectionCoeff |
                DirtyFlags.MaterialColor;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.WorldView) != 0)
            {
                Matrix.Multiply(ref world, ref view, out worldView);

                Matrix transposeWorldView;
                Matrix.Transpose(ref worldView, out transposeWorldView);

                parametersPerObjectVS.WorldView = transposeWorldView;
                parametersPerObjectPS.WorldView = transposeWorldView;

                dirtyFlags &= ~DirtyFlags.WorldView;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectVS;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
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

            if ((dirtyFlags & DirtyFlags.MaterialColor) != 0)
            {
                parametersPerObjectPS.DiffuseColor.X = diffuseColor.X * alpha;
                parametersPerObjectPS.DiffuseColor.Y = diffuseColor.Y * alpha;
                parametersPerObjectPS.DiffuseColor.Z = diffuseColor.Z * alpha;
                parametersPerObjectPS.DiffuseColor.W = alpha;

                parametersPerObjectPS.EmissiveColor.X = (emissiveColor.X + ambientLightColor.X * diffuseColor.X) * alpha;
                parametersPerObjectPS.EmissiveColor.Y = (emissiveColor.Y + ambientLightColor.Y * diffuseColor.Y) * alpha;
                parametersPerObjectPS.EmissiveColor.Z = (emissiveColor.Z + ambientLightColor.Z * diffuseColor.Z) * alpha;

                dirtyFlags &= ~DirtyFlags.MaterialColor;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObjectVS) != 0)
            {
                constantBufferPerObjectVS.SetData(context, parametersPerObjectVS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObjectVS;
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

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerSceneVS) != 0)
            {
                constantBufferPerSceneVS.SetData(context, parametersPerSceneVS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerSceneVS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerScenePS) != 0)
            {
                constantBufferPerScenePS.SetData(context, parametersPerScenePS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerScenePS;
            }

            context.VertexShader = sharedDeviceResource.VertexShader;
            context.VertexShaderConstantBuffers[0] = constantBufferPerObjectVS;
            context.VertexShaderConstantBuffers[1] = constantBufferPerSceneVS;

            context.PixelShader = sharedDeviceResource.PixelShader;
            context.PixelShaderConstantBuffers[0] = constantBufferPerObjectPS;
            context.PixelShaderConstantBuffers[1] = constantBufferPerFramePS;
            context.PixelShaderConstantBuffers[2] = constantBufferPerScenePS;
            context.PixelShaderResources[0] = NormalMap0;
            context.PixelShaderResources[1] = NormalMap1;
            context.PixelShaderResources[2] = ReflectionMap;
            context.PixelShaderResources[3] = RefractionMap;
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
