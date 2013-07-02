#region Using

using System;
using System.Runtime.InteropServices;
using Libra;
using Libra.Graphics;
using Libra.Graphics.Compiler;

#endregion

namespace Samples.SceneGodRay
{
    public sealed class SkySphereEffect : IEffect, IEffectMatrices, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                var compiler = ShaderCompiler.CreateShaderCompiler();
                compiler.RootPath = "../../Shaders";
                compiler.OptimizationLevel = OptimizationLevels.Level3;
                compiler.EnableStrictness = true;
                compiler.WarningsAreErrors = true;

                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(compiler.CompileVertexShader("SkySphere.hlsl"));

                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(compiler.CompilePixelShader("SkySphere.hlsl"));
            }
        }

        #endregion

        #region VSConstants

        public struct VSConstants
        {
            public Matrix WorldViewProjection;
        }

        #endregion

        #region PSConstants

        [StructLayout(LayoutKind.Explicit, Size = 64)]
        public struct PSConstants
        {
            [FieldOffset(0)]
            public Vector3 SkyColor;

            [FieldOffset(16)]
            public Vector3 SunDirection;

            [FieldOffset(32)]
            public Vector3 SunColor;

            [FieldOffset(48)]
            public float SunThreshold;

            [FieldOffset(52)]
            public float SunVisible;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ViewProjection      = (1 << 0),
            WorldViewProjection = (1 << 1),
            VSConstants         = (1 << 2),
            PSConstants         = (1 << 3)
        }

        #endregion

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer vsConstantBuffer;

        ConstantBuffer psConstantBuffer;

        VSConstants vsConstants;

        PSConstants psConstants;

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

                dirtyFlags |= DirtyFlags.WorldViewProjection;
            }
        }

        public Matrix View
        {
            get { return view; }
            set
            {
                view = value;

                dirtyFlags |= DirtyFlags.ViewProjection;
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

        public Vector3 SkyColor
        {
            get { return psConstants.SkyColor; }
            set
            {
                psConstants.SkyColor = value;

                dirtyFlags |= DirtyFlags.PSConstants;
            }
        }

        public Vector3 SunDirection
        {
            get { return psConstants.SunDirection; }
            set
            {
                psConstants.SunDirection = value;

                dirtyFlags |= DirtyFlags.PSConstants;
            }
        }

        public Vector3 SunColor
        {
            get { return psConstants.SunColor; }
            set
            {
                psConstants.SunColor = value;

                dirtyFlags |= DirtyFlags.PSConstants;
            }
        }

        public float SunThreshold
        {
            get { return psConstants.SunThreshold; }
            set
            {
                psConstants.SunThreshold = value;

                dirtyFlags |= DirtyFlags.PSConstants;
            }
        }

        public bool SunVisible
        {
            get { return psConstants.SunVisible != 0.0f; }
            set
            {
                psConstants.SunVisible = (value) ? 1.0f : 0.0f;

                dirtyFlags |= DirtyFlags.PSConstants;
            }
        }

        public SkySphereEffect(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<SkySphereEffect, SharedDeviceResource>();

            vsConstantBuffer = deviceContext.Device.CreateConstantBuffer();
            vsConstantBuffer.Initialize<VSConstants>();

            psConstantBuffer = deviceContext.Device.CreateConstantBuffer();
            psConstantBuffer.Initialize<PSConstants>();

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;
            viewProjection = Matrix.Identity;

            vsConstants.WorldViewProjection = Matrix.Identity;
            psConstants.SkyColor = Vector3.Zero;
            psConstants.SunDirection = Vector3.Up;
            psConstants.SunColor = Vector3.One;
            psConstants.SunThreshold = 0.999f;
            psConstants.SunVisible = 1.0f;

            dirtyFlags = DirtyFlags.VSConstants | DirtyFlags.PSConstants;
        }

        public void Apply()
        {
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

                Matrix.Transpose(ref worldViewProjection, out vsConstants.WorldViewProjection);

                dirtyFlags &= ~DirtyFlags.WorldViewProjection;
                dirtyFlags |= DirtyFlags.VSConstants;
            }

            if ((dirtyFlags & DirtyFlags.VSConstants) != 0)
            {
                vsConstantBuffer.SetData(DeviceContext, vsConstants);

                dirtyFlags &= ~DirtyFlags.VSConstants;
            }

            if ((dirtyFlags & DirtyFlags.PSConstants) != 0)
            {
                psConstantBuffer.SetData(DeviceContext, psConstants);

                dirtyFlags &= ~DirtyFlags.PSConstants;
            }

            DeviceContext.VertexShaderConstantBuffers[0] = vsConstantBuffer;
            DeviceContext.VertexShader = sharedDeviceResource.VertexShader;
            DeviceContext.PixelShaderConstantBuffers[0] = psConstantBuffer;
            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
        }

        #region IDisposable

        bool disposed;

        ~SkySphereEffect()
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
                vsConstantBuffer.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
