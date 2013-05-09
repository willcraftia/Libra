#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class SingleColorObjectEffect : IEffect, IEffectMatrices, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(Resources.SingleColorObjectVS);

                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.SingleColorObjectPS);
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

        public struct PSConstants
        {
            public Vector4 Color;
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

        Device device;

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

        public Vector4 Color
        {
            get { return psConstants.Color; }
            set
            {
                psConstants.Color = value;

                dirtyFlags |= DirtyFlags.PSConstants;
            }
        }

        public SingleColorObjectEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<SingleColorObjectEffect, SharedDeviceResource>();

            vsConstantBuffer = device.CreateConstantBuffer();
            vsConstantBuffer.Initialize<VSConstants>();

            psConstantBuffer = device.CreateConstantBuffer();
            psConstantBuffer.Initialize<PSConstants>();

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;
            viewProjection = Matrix.Identity;

            vsConstants.WorldViewProjection = Matrix.Identity;
            psConstants.Color = Vector4.Zero;

            dirtyFlags = DirtyFlags.VSConstants | DirtyFlags.PSConstants;
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

                Matrix.Transpose(ref worldViewProjection, out vsConstants.WorldViewProjection);

                dirtyFlags &= ~DirtyFlags.WorldViewProjection;
                dirtyFlags |= DirtyFlags.VSConstants;
            }

            if ((dirtyFlags & DirtyFlags.VSConstants) != 0)
            {
                vsConstantBuffer.SetData(context, vsConstants);

                dirtyFlags &= ~DirtyFlags.VSConstants;
            }

            if ((dirtyFlags & DirtyFlags.PSConstants) != 0)
            {
                psConstantBuffer.SetData(context, psConstants);

                dirtyFlags &= ~DirtyFlags.PSConstants;
            }

            context.VertexShaderConstantBuffers[0] = vsConstantBuffer;
            context.VertexShader = sharedDeviceResource.VertexShader;
            context.PixelShaderConstantBuffers[0] = psConstantBuffer;
            context.PixelShader = sharedDeviceResource.PixelShader;
        }

        #region IDisposable

        bool disposed;

        ~SingleColorObjectEffect()
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
