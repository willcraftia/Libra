#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class EdgeFilter : IFilterEffect, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.EdgeFilterPS);
            }
        }

        #endregion

        #region ParametersPerObject

        [StructLayout(LayoutKind.Explicit, Size = 48)]
        public struct ParametersPerObject
        {
            [FieldOffset(0)]
            public Vector3 EdgeColor;

            [FieldOffset(12)]
            public float EdgeIntensity;

            [FieldOffset(16)]
            public float DepthThreshold;

            [FieldOffset(20)]
            public float DepthSensitivity;

            [FieldOffset(24)]
            public float NormalThreshold;

            [FieldOffset(28)]
            public float NormalSensitivity;

            [FieldOffset(32)]
            public float Attenuation;
        }

        #endregion

        #region ParametersPerRenderTarget

        [StructLayout(LayoutKind.Sequential, Size = 16 * KernelSize)]
        public struct ParametersPerRenderTarget
        {
            // XY: テクセル オフセット
            // ZW: 整列用ダミー
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = KernelSize)]
            public Vector4[] Offsets;
        }

        #endregion

        #region ParametersPerCamera

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct ParametersPerCamera
        {
            [FieldOffset(0)]
            public float NearClipDistance;

            [FieldOffset(4)]
            public float FarClipDistance;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObject         = (1 << 0),
            ConstantBufferPerRenderTarget   = (1 << 1),
            ConstantBufferPerCamera         = (1 << 2),
            Offsets                         = (1 << 3)
        }

        #endregion

        const int KernelSize = 4;

        static readonly Vector2[] PixelOffsets =
        {
            new Vector2(-1, -1),
            new Vector2( 1,  1),
            new Vector2(-1,  1),
            new Vector2( 1, -1)
        };

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerObject;

        ConstantBuffer constantBufferPerRenderTarget;

        ConstantBuffer constantBufferPerCamera;

        ParametersPerObject parametersPerObject;

        ParametersPerRenderTarget parametersPerRenderTarget;

        ParametersPerCamera parametersPerCamera;

        float edgeWidth;

        int viewportWidth;

        int viewportHeight;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public float EdgeWidth
        {
            get { return edgeWidth; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                edgeWidth = value;

                dirtyFlags |= DirtyFlags.Offsets;
            }
        }

        public float EdgeIntensity
        {
            get { return parametersPerObject.EdgeIntensity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.EdgeIntensity = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float EdgeAttenuation
        {
            get { return parametersPerObject.Attenuation; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.Attenuation = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float NormalThreshold
        {
            get { return parametersPerObject.NormalThreshold; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.NormalThreshold = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float DepthThreshold
        {
            get { return parametersPerObject.DepthThreshold; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.DepthThreshold = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float NormalSensitivity
        {
            get { return parametersPerObject.NormalSensitivity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.NormalSensitivity = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float DepthSensitivity
        {
            get { return parametersPerObject.DepthSensitivity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.DepthSensitivity = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float NearClipDistance
        {
            get { return parametersPerCamera.NearClipDistance; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerCamera.NearClipDistance = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerCamera;
            }
        }

        public float FarClipDistance
        {
            get { return parametersPerCamera.FarClipDistance; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerCamera.FarClipDistance = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerCamera;
            }
        }

        public ShaderResourceView LinearDepthMap { get; set; }

        public ShaderResourceView NormalMap { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public SamplerState NormalMapSampler { get; set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public EdgeFilter(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<EdgeFilter, SharedDeviceResource>();

            constantBufferPerObject = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObject.Initialize<ParametersPerObject>();

            constantBufferPerRenderTarget = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerRenderTarget.Initialize<ParametersPerRenderTarget>();

            constantBufferPerCamera = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerCamera.Initialize<ParametersPerCamera>();

            edgeWidth = 1;
            viewportWidth = 1;
            viewportHeight = 1;

            parametersPerObject.EdgeIntensity = 3.0f;
            parametersPerObject.EdgeColor = Vector3.Zero;
            parametersPerObject.DepthThreshold = 5.0f;
            parametersPerObject.DepthSensitivity = 1.0f;
            parametersPerObject.NormalThreshold = 0.2f;
            parametersPerObject.NormalSensitivity = 10.0f;
            parametersPerObject.Attenuation = 0.8f;
            
            parametersPerCamera.NearClipDistance = 1.0f;
            parametersPerCamera.FarClipDistance = 1000.0f;
            
            parametersPerRenderTarget.Offsets = new Vector4[KernelSize];

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerObject |
                DirtyFlags.ConstantBufferPerRenderTarget |
                DirtyFlags.ConstantBufferPerCamera |
                DirtyFlags.Offsets;
        }

        public void Apply()
        {
            var viewport = DeviceContext.Viewport;
            int w = (int) viewport.Width;
            int h = (int) viewport.Height;

            if (w != viewportWidth || h != viewportHeight)
            {
                viewportWidth = w;
                viewportHeight = h;

                dirtyFlags |= DirtyFlags.Offsets;
            }

            if ((dirtyFlags & DirtyFlags.Offsets) != 0)
            {
                float scaleX = edgeWidth / (float) viewportWidth;
                float scaleY = edgeWidth / (float) viewportHeight;

                for (int i = 0; i < KernelSize; i++)
                {
                    parametersPerRenderTarget.Offsets[i].X = PixelOffsets[i].X * scaleX;
                    parametersPerRenderTarget.Offsets[i].Y = PixelOffsets[i].Y * scaleY;
                }

                dirtyFlags &= ~DirtyFlags.Offsets;
                dirtyFlags |= DirtyFlags.ConstantBufferPerRenderTarget;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObject) != 0)
            {
                constantBufferPerObject.SetData(DeviceContext, parametersPerObject);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObject;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerRenderTarget) != 0)
            {
                constantBufferPerRenderTarget.SetData(DeviceContext, parametersPerRenderTarget);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerRenderTarget;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerCamera) != 0)
            {
                constantBufferPerCamera.SetData(DeviceContext, parametersPerCamera);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerCamera;
            }

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerObject;
            DeviceContext.PixelShaderConstantBuffers[1] = constantBufferPerRenderTarget;
            DeviceContext.PixelShaderConstantBuffers[2] = constantBufferPerCamera;
            DeviceContext.PixelShaderResources[0] = Texture;
            DeviceContext.PixelShaderResources[1] = LinearDepthMap;
            DeviceContext.PixelShaderResources[2] = NormalMap;
            DeviceContext.PixelShaderSamplers[0] = TextureSampler;
            DeviceContext.PixelShaderSamplers[1] = LinearDepthMapSampler;
            DeviceContext.PixelShaderSamplers[2] = NormalMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~EdgeFilter()
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
                constantBufferPerObject.Dispose();
                constantBufferPerRenderTarget.Dispose();
                constantBufferPerCamera.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
