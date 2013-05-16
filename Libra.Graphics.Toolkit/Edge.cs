#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class Edge : IPostprocessPass, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.EdgePS);
            }
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Explicit, Size = 48 + 16 * KernelSize)]
        public struct Constants
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
            public float NearClipDistance;

            [FieldOffset(36)]
            public float FarClipDistance;

            [FieldOffset(40)]
            public float Attenuation;

            // XY のみ有効 (ZW は整列用ダミー)。
            [FieldOffset(48), MarshalAs(UnmanagedType.ByValArray, SizeConst = KernelSize)]
            public Vector4[] Kernel;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            Kernel  = (1 << 0),
            Constants   = (1 << 1)
        }

        #endregion

        const int KernelSize = 4;

        static readonly Vector2[] PixelKernel =
        {
            new Vector2(-1, -1),
            new Vector2( 1,  1),
            new Vector2(-1,  1),
            new Vector2( 1, -1)
        };

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

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

                dirtyFlags |= DirtyFlags.Kernel;
            }
        }

        public float EdgeIntensity
        {
            get { return constants.EdgeIntensity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.EdgeIntensity = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float EdgeAttenuation
        {
            get { return constants.Attenuation; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                constants.Attenuation = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float NormalThreshold
        {
            get { return constants.NormalThreshold; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                constants.NormalThreshold = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float DepthThreshold
        {
            get { return constants.DepthThreshold; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                constants.DepthThreshold = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float NormalSensitivity
        {
            get { return constants.NormalSensitivity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.NormalSensitivity = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float DepthSensitivity
        {
            get { return constants.DepthSensitivity; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.DepthSensitivity = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float NearClipDistance
        {
            get { return constants.NearClipDistance; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.NearClipDistance = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float FarClipDistance
        {
            get { return constants.FarClipDistance; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.FarClipDistance = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public ShaderResourceView LinearDepthMap { get; set; }

        public ShaderResourceView NormalMap { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public SamplerState NormalMapSampler { get; set; }

        public bool Enabled { get; set; }

        public Edge(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<Edge, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            edgeWidth = 1;
            viewportWidth = 1;
            viewportHeight = 1;
            constants.EdgeIntensity = 3.0f;
            constants.EdgeColor = Vector3.Zero;
            constants.DepthThreshold = 5.0f;
            constants.DepthSensitivity = 1.0f;
            constants.NormalThreshold = 0.2f;
            constants.NormalSensitivity = 10.0f;
            constants.NearClipDistance = 1.0f;
            constants.FarClipDistance = 1000.0f;
            constants.Attenuation = 0.8f;
            constants.Kernel = new Vector4[KernelSize];

            LinearDepthMapSampler = SamplerState.LinearClamp;
            NormalMapSampler = SamplerState.LinearClamp;

            Enabled = true;

            dirtyFlags = DirtyFlags.Kernel | DirtyFlags.Constants;
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

                dirtyFlags |= DirtyFlags.Kernel;
            }

            if ((dirtyFlags & DirtyFlags.Kernel) != 0)
            {
                float scaleX = edgeWidth / (float) viewportWidth;
                float scaleY = edgeWidth / (float) viewportHeight;

                for (int i = 0; i < KernelSize; i++)
                {
                    constants.Kernel[i].X = PixelKernel[i].X * scaleX;
                    constants.Kernel[i].Y = PixelKernel[i].Y * scaleY;
                }

                dirtyFlags &= ~DirtyFlags.Kernel;
                dirtyFlags |= DirtyFlags.Constants;
            }

            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constantBuffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            context.PixelShaderConstantBuffers[0] = constantBuffer;
            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[1] = LinearDepthMap;
            context.PixelShaderResources[2] = NormalMap;
            context.PixelShaderSamplers[1] = LinearDepthMapSampler;
            context.PixelShaderSamplers[2] = NormalMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~Edge()
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
