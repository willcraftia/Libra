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

        #region ParametersPerShader

        [StructLayout(LayoutKind.Explicit, Size = 48)]
        public struct ParametersPerShader
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
            ConstantsPerShader          = (1 << 0),
            ConstantsPerRenderTarget    = (1 << 1),
            ConstantsPerCamera          = (1 << 2),
            Offsets                     = (1 << 3)
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

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerShader;

        ConstantBuffer constantBufferPerRenderTarget;

        ConstantBuffer constantBufferPerCamera;

        ParametersPerShader parametersPerShader;

        ParametersPerRenderTarget parametersPerRenderTarget;

        ParametersPerCamera parametersPerCamera;

        float edgeWidth;

        int viewportWidth;

        int viewportHeight;

        DirtyFlags dirtyFlags;

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
            get { return parametersPerShader.EdgeIntensity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.EdgeIntensity = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        public float EdgeAttenuation
        {
            get { return parametersPerShader.Attenuation; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.Attenuation = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        public float NormalThreshold
        {
            get { return parametersPerShader.NormalThreshold; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.NormalThreshold = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        public float DepthThreshold
        {
            get { return parametersPerShader.DepthThreshold; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.DepthThreshold = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        public float NormalSensitivity
        {
            get { return parametersPerShader.NormalSensitivity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.NormalSensitivity = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        public float DepthSensitivity
        {
            get { return parametersPerShader.DepthSensitivity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerShader.DepthSensitivity = value;

                dirtyFlags |= DirtyFlags.ConstantsPerShader;
            }
        }

        public float NearClipDistance
        {
            get { return parametersPerCamera.NearClipDistance; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerCamera.NearClipDistance = value;

                dirtyFlags |= DirtyFlags.ConstantsPerCamera;
            }
        }

        public float FarClipDistance
        {
            get { return parametersPerCamera.FarClipDistance; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerCamera.FarClipDistance = value;

                dirtyFlags |= DirtyFlags.ConstantsPerCamera;
            }
        }

        public ShaderResourceView LinearDepthMap { get; set; }

        public ShaderResourceView NormalMap { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public SamplerState NormalMapSampler { get; set; }

        public bool Enabled { get; set; }

        public EdgeFilter(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<EdgeFilter, SharedDeviceResource>();

            constantBufferPerShader = device.CreateConstantBuffer();
            constantBufferPerShader.Initialize<ParametersPerShader>();

            constantBufferPerRenderTarget = device.CreateConstantBuffer();
            constantBufferPerRenderTarget.Initialize<ParametersPerRenderTarget>();

            constantBufferPerCamera = device.CreateConstantBuffer();
            constantBufferPerCamera.Initialize<ParametersPerCamera>();

            edgeWidth = 1;
            viewportWidth = 1;
            viewportHeight = 1;

            parametersPerShader.EdgeIntensity = 3.0f;
            parametersPerShader.EdgeColor = Vector3.Zero;
            parametersPerShader.DepthThreshold = 5.0f;
            parametersPerShader.DepthSensitivity = 1.0f;
            parametersPerShader.NormalThreshold = 0.2f;
            parametersPerShader.NormalSensitivity = 10.0f;
            parametersPerShader.Attenuation = 0.8f;
            
            parametersPerCamera.NearClipDistance = 1.0f;
            parametersPerCamera.FarClipDistance = 1000.0f;
            
            parametersPerRenderTarget.Offsets = new Vector4[KernelSize];

            LinearDepthMapSampler = SamplerState.LinearClamp;
            NormalMapSampler = SamplerState.LinearClamp;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantsPerShader |
                DirtyFlags.ConstantsPerRenderTarget |
                DirtyFlags.ConstantsPerCamera |
                DirtyFlags.Offsets;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var viewport = context.Viewport;
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
                dirtyFlags |= DirtyFlags.ConstantsPerRenderTarget;
            }

            if ((dirtyFlags & DirtyFlags.ConstantsPerShader) != 0)
            {
                constantBufferPerShader.SetData(context, parametersPerShader);

                dirtyFlags &= ~DirtyFlags.ConstantsPerShader;
            }

            if ((dirtyFlags & DirtyFlags.ConstantsPerRenderTarget) != 0)
            {
                constantBufferPerRenderTarget.SetData(context, parametersPerRenderTarget);

                dirtyFlags &= ~DirtyFlags.ConstantsPerRenderTarget;
            }

            if ((dirtyFlags & DirtyFlags.ConstantsPerCamera) != 0)
            {
                constantBufferPerCamera.SetData(context, parametersPerCamera);

                dirtyFlags &= ~DirtyFlags.ConstantsPerCamera;
            }

            context.PixelShaderConstantBuffers[0] = constantBufferPerShader;
            context.PixelShaderConstantBuffers[1] = constantBufferPerRenderTarget;
            context.PixelShaderConstantBuffers[2] = constantBufferPerCamera;
            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[1] = LinearDepthMap;
            context.PixelShaderResources[2] = NormalMap;
            context.PixelShaderSamplers[1] = LinearDepthMapSampler;
            context.PixelShaderSamplers[2] = NormalMapSampler;
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
                constantBufferPerShader.Dispose();
                constantBufferPerRenderTarget.Dispose();
                constantBufferPerCamera.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
