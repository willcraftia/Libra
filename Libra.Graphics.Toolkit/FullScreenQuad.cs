#region Using

using System;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class FullScreenQuad : IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public VertexShader VertexShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                VertexShader = device.CreateVertexShader();
                VertexShader.Name = "FullScreenQuadVS";
                VertexShader.Initialize(Resources.FullScreenQuadVS);
            }
        }

        #endregion

        DeviceContext context;

        SharedDeviceResource sharedDeviceResource;

        public FullScreenQuad(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.context = context;

            sharedDeviceResource = context.Device.GetSharedResource<FullScreenQuad, SharedDeviceResource>();
        }

        public void Draw()
        {
            context.VertexShader = sharedDeviceResource.VertexShader;
            context.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.Draw(3);
        }

        #region IDisposable

        bool disposed;

        ~FullScreenQuad()
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
            }

            disposed = true;
        }

        #endregion
    }
}
