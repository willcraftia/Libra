#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class FullScreenQuad : IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            Device device;

            VertexShader vertexShader;

            VertexShader viewRayVertexShader;

            public VertexShader VertexShader
            {
                get
                {
                    if (vertexShader == null)
                    {
                        vertexShader = device.CreateVertexShader();
                        vertexShader.Name = "FullScreenQuadVS";
                        vertexShader.Initialize(Resources.FullScreenQuadVS);
                    }

                    return vertexShader;
                }
            }

            public VertexShader ViewRayVertexShader
            {
                get
                {
                    if (viewRayVertexShader == null)
                    {
                        viewRayVertexShader = device.CreateVertexShader();
                        viewRayVertexShader.Name = "FullScreenQuadViewRayVS";
                        viewRayVertexShader.Initialize(Resources.FullScreenQuadViewRayVS);
                    }

                    return viewRayVertexShader;
                }
            }

            public SharedDeviceResource(Device device)
            {
                this.device = device;
            }
        }

        #endregion

        #region ParametersPerCamera

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct ParametersPerCamera
        {
            public Vector2 FocalLength;
        }

        #endregion

        DeviceContext context;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerCamera;

        ParametersPerCamera parametersPerCamera;

        Matrix projection;

        bool constantBufferPerCameraDirty;

        public Matrix Projection
        {
            get { return projection; }
            set
            {
                projection = value;
                parametersPerCamera.FocalLength.X = projection.M11;
                parametersPerCamera.FocalLength.Y = projection.M22;

                constantBufferPerCameraDirty = true;
            }
        }

        public bool ViewRayEnabled { get; set; }

        public FullScreenQuad(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.context = context;

            sharedDeviceResource = context.Device.GetSharedResource<FullScreenQuad, SharedDeviceResource>();

            parametersPerCamera.FocalLength = Vector2.One;

            constantBufferPerCameraDirty = true;
        }

        public void Draw()
        {
            if (ViewRayEnabled)
            {
                if (constantBufferPerCamera == null)
                {
                    constantBufferPerCamera = context.Device.CreateConstantBuffer();
                    constantBufferPerCamera.Initialize<ParametersPerCamera>();
                }

                if (constantBufferPerCameraDirty)
                {
                    constantBufferPerCamera.SetData(context, parametersPerCamera);
                    constantBufferPerCameraDirty = false;
                }

                context.VertexShaderConstantBuffers[0] = constantBufferPerCamera;
                context.VertexShader = sharedDeviceResource.ViewRayVertexShader;
            }
            else
            {
                context.VertexShader = sharedDeviceResource.VertexShader;
            }

            // 入力レイアウト自動解決を OFF に。
            context.AutoResolveInputLayout = false;

            context.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.Draw(3);

            context.AutoResolveInputLayout = true;
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

                if (constantBufferPerCamera != null)
                    constantBufferPerCamera.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
