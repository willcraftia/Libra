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

        #region Constants

        [StructLayout(LayoutKind.Explicit, Size = 128)]
        public struct Constants
        {
            [FieldOffset(0)]
            public Matrix WorldViewProjection;

            [FieldOffset(64)]
            public Vector3 SkyColor;

            [FieldOffset(80)]
            public Vector3 SunDirection;

            [FieldOffset(96)]
            public Vector3 SunColor;

            [FieldOffset(112)]
            public float SunThreshold;

            [FieldOffset(116)]
            public float SunVisible;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ViewProjection      = (1 << 0),
            WorldViewProjection = (1 << 1),
            Constants           = (1 << 2)
        }

        #endregion

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

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
            get { return constants.SkyColor; }
            set
            {
                constants.SkyColor = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public Vector3 SunDirection
        {
            get { return constants.SunDirection; }
            set
            {
                constants.SunDirection = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public Vector3 SunColor
        {
            get { return constants.SunColor; }
            set
            {
                constants.SunColor = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float SunThreshold
        {
            get { return constants.SunThreshold; }
            set
            {
                constants.SunThreshold = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public bool SunVisible
        {
            get { return constants.SunVisible != 0.0f; }
            set
            {
                constants.SunVisible = (value) ? 1.0f : 0.0f;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public SkySphereEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<SkySphereEffect, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;
            viewProjection = Matrix.Identity;

            constants.WorldViewProjection = Matrix.Identity;
            constants.SkyColor = Vector3.Zero;
            constants.SunDirection = Vector3.Up;
            constants.SunColor = Vector3.One;
            constants.SunThreshold = 0.999f;
            constants.SunVisible = 1.0f;

            dirtyFlags = DirtyFlags.Constants;
        }

        public void Apply(DeviceContext context)
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

                Matrix.Transpose(ref worldViewProjection, out constants.WorldViewProjection);

                dirtyFlags &= ~DirtyFlags.WorldViewProjection;
                dirtyFlags |= DirtyFlags.Constants;
            }

            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constantBuffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            context.VertexShaderConstantBuffers[0] = constantBuffer;
            context.VertexShader = sharedDeviceResource.VertexShader;
            context.PixelShader = sharedDeviceResource.PixelShader;
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
                constantBuffer.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
