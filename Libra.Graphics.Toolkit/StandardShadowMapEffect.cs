#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class StandardShadowMapEffect : IDisposable
    {
        #region DeviceResources

        sealed class DeviceResources
        {
            Device device;

            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            internal DeviceResources(Device device)
            {
                this.device = device;

                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(Resources.StandardShadowMapVS);

                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.StandardShadowMapPS);
            }
        }

        #endregion

        #region Constants

        public struct Constants
        {
            public Matrix World;

            public Matrix LightViewProjection;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            World               = (1 << 0),
            LightViewProjection = (1 << 1),
            Constants           = (1 << 2)
        }

        #endregion

        static readonly SharedResourcePool<Device, DeviceResources> DeviceResourcesPool;

        Device device;

        DeviceResources deviceResources;

        ConstantBuffer constantBuffer;

        Constants constants;

        Matrix world;

        Matrix lightViewProjection;

        DirtyFlags dirtyFlags;

        public Matrix World
        {
            get { return world; }
            set
            {
                if (world == value) return;

                world = value;

                dirtyFlags |= DirtyFlags.World;
            }
        }

        public Matrix LightViewProjection
        {
            get { return lightViewProjection; }
            set
            {
                if (lightViewProjection == value) return;

                lightViewProjection = value;

                dirtyFlags |= DirtyFlags.LightViewProjection;
            }
        }

        static StandardShadowMapEffect()
        {
            DeviceResourcesPool = new SharedResourcePool<Device, DeviceResources>(
                (device) => { return new DeviceResources(device); });
        }

        public StandardShadowMapEffect(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            deviceResources = DeviceResourcesPool.Get(device);

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            world = Matrix.Identity;
            lightViewProjection = Matrix.Identity;

            dirtyFlags = DirtyFlags.World | DirtyFlags.LightViewProjection;
        }

        public void Apply(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.World) != 0)
            {
                Matrix.Transpose(ref world, out constants.World);

                dirtyFlags &= ~DirtyFlags.World;
                dirtyFlags |= DirtyFlags.Constants;
            }

            if ((dirtyFlags & DirtyFlags.LightViewProjection) != 0)
            {
                Matrix.Transpose(ref lightViewProjection, out constants.LightViewProjection);

                dirtyFlags &= ~DirtyFlags.LightViewProjection;
                dirtyFlags |= DirtyFlags.Constants;
            }

            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constantBuffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            context.VertexShaderConstantBuffers[0] = constantBuffer;
            context.VertexShader = deviceResources.VertexShader;
            context.PixelShader = deviceResources.PixelShader;
        }

        #region IDisposable

        bool disposed;

        ~StandardShadowMapEffect()
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
                deviceResources = null;
                constantBuffer.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
