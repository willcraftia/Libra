#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class AmbientOcclusionMap : IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.AmbientOcclusionMapPS);
            }
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Explicit, Size = 32 + 16 * MaxSampleCount)]
        public struct Constants
        {
            [FieldOffset(0)]
            public float Strength;

            [FieldOffset(4)]
            public float Attenuation;

            [FieldOffset(8)]
            public float Radius;

            [FieldOffset(16)]
            public Vector2 RandomOffset;

            [FieldOffset(24)]
            public float SampleCount;

            [FieldOffset(32), MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxSampleCount)]
            public Vector3[] SampleSphere;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            Random              = (1 << 0),
            RandomNormalOffset  = (1 << 1),
            SampleSphere        = (1 << 2),
            Constants           = (1 << 3)
        }

        #endregion

        public const int MaxSampleCount = 128;

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

        int seed;

        Random random;

        int width;

        int height;

        DirtyFlags dirtyFlags;

        public int Seed
        {
            get { return seed; }
            set
            {
                seed = value;

                dirtyFlags |= DirtyFlags.Random;
            }
        }

        public float Strength
        {
            get { return constants.Strength; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.Strength = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float Attenuation
        {
            get { return constants.Attenuation; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.Attenuation = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public float Radius
        {
            get { return constants.Radius; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                constants.Radius = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public int SampleCount
        {
            get { return (int) constants.SampleCount; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                constants.SampleCount = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public int Width
        {
            get { return width; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                width = value;

                dirtyFlags |= DirtyFlags.RandomNormalOffset;
            }
        }

        public int Height
        {
            get { return height; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                height = value;

                dirtyFlags |= DirtyFlags.RandomNormalOffset;
            }
        }

        public ShaderResourceView LinearDepthMap { get; set; }

        public ShaderResourceView NormalMap { get; set; }

        public ShaderResourceView RandomNormalMap { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public SamplerState NormalMapSampler { get; set; }

        public SamplerState RandomNormalMapSampler { get; private set; }

        public bool Enabled { get; set; }

        public AmbientOcclusionMap(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<AmbientOcclusionMap, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            seed = 0;
            width = 1;
            height = 1;

            constants.Strength = 20.0f;
            constants.Attenuation = 0.5f;
            constants.Radius = 10.0f;
            constants.RandomOffset = Vector2.One;
            constants.SampleCount = 16;
            constants.SampleSphere = new Vector3[MaxSampleCount];

            LinearDepthMapSampler = SamplerState.PointClamp;
            NormalMapSampler = SamplerState.PointClamp;
            RandomNormalMapSampler = SamplerState.PointWrap;

            Enabled = true;

            dirtyFlags = DirtyFlags.Random | DirtyFlags.RandomNormalOffset | DirtyFlags.SampleSphere | DirtyFlags.Constants;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            SetSampleSphere();
            SetRandomNormalOffset();

            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constantBuffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            context.PixelShaderConstantBuffers[0] = constantBuffer;
            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[1] = LinearDepthMap;
            context.PixelShaderResources[2] = NormalMap;
            context.PixelShaderResources[3] = RandomNormalMap;
            context.PixelShaderSamplers[1] = LinearDepthMapSampler;
            context.PixelShaderSamplers[2] = NormalMapSampler;
            context.PixelShaderSamplers[3] = RandomNormalMapSampler;
        }

        void SetSampleSphere()
        {
            if ((dirtyFlags & DirtyFlags.Random) != 0)
            {
                random = new Random(seed);

                dirtyFlags &= ~DirtyFlags.Random;
                dirtyFlags |= DirtyFlags.SampleSphere;
            }

            if ((dirtyFlags & DirtyFlags.SampleSphere) != 0)
            {
                for (int i = 0; i < MaxSampleCount; i++)
                {
                    // 単位球面上のランダム点。
                    var vector = new Vector3
                    {
                        X = (float) random.NextDouble() * 2.0f - 1.0f,
                        Y = (float) random.NextDouble() * 2.0f - 1.0f,
                        Z = (float) random.NextDouble() * 2.0f - 1.0f
                    };
                    vector.Normalize();

                    // 単位球面内のランダム点。
                    Vector3.Multiply(ref vector, (float) random.NextDouble(), out vector);

                    constants.SampleSphere[i] = vector;
                }

                dirtyFlags &= ~DirtyFlags.SampleSphere;
                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        void SetRandomNormalOffset()
        {
            if ((dirtyFlags & DirtyFlags.RandomNormalOffset) != 0)
            {
                int w;
                int h;
                GetTextureSize(RandomNormalMap, out w, out h);

                constants.RandomOffset.X = (float) width / (float) w;
                constants.RandomOffset.Y = (float) height / (float) h;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        static void GetTextureSize(ShaderResourceView shaderResourceView, out int width, out int height)
        {
            var texture = shaderResourceView.Resource as Texture2D;
            if (texture == null)
                throw new ArgumentException("ShaderResourceView is not for Texture2D.", "shaderResourceView");

            width = texture.Width;
            height = texture.Height;
        }

        #region IDisposable

        bool disposed;

        ~AmbientOcclusionMap()
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
