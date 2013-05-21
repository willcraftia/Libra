#region Using

using System;
using System.Runtime.InteropServices;
using Libra.PackedVector;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class SSAOMap : IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(Resources.SSAOMapVS);
                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.SSAOMapPS);
            }
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Explicit, Size = 32 + 16 * MaxSampleCount)]
        public struct Constants
        {
            [FieldOffset(0)]
            public Vector2 FocalLength;

            [FieldOffset(8)]
            public float SampleCount;

            [FieldOffset(16)]
            public float Strength;

            [FieldOffset(20)]
            public float Attenuation;

            [FieldOffset(24)]
            public float Radius;

            [FieldOffset(28)]
            public float FarClipDistance;

            [FieldOffset(32), MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxSampleCount)]
            public Vector4[] SampleSphere;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            Random          = (1 << 0),
            RandomNormals   = (1 << 1),
            SampleSphere    = (1 << 2),
            Projection      = (1 << 3),
            Constants       = (1 << 4)
        }

        #endregion

        public const int MaxSampleCount = 128;

        const int RandomNormalMapSize = 64;

        Device device;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBuffer;

        Constants constants;

        int seed;

        Random random;

        Texture2D randomNormalMap;
        
        Matrix projection;

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
                if (value < 1 || MaxSampleCount < value) throw new ArgumentOutOfRangeException("value");

                constants.SampleCount = value;

                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        public Matrix Projection
        {
            get { return projection; }
            set
            {
                projection = value;

                dirtyFlags |= DirtyFlags.Projection;
            }
        }

        public ShaderResourceView LinearDepthMap { get; set; }

        public ShaderResourceView NormalMap { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public SamplerState NormalMapSampler { get; set; }

        public bool Enabled { get; set; }

        public SSAOMap(Device device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            sharedDeviceResource = device.GetSharedResource<SSAOMap, SharedDeviceResource>();

            constantBuffer = device.CreateConstantBuffer();
            constantBuffer.Initialize<Constants>();

            seed = 0;

            constants.FocalLength = Vector2.One;
            constants.Strength = 5.0f;
            constants.Attenuation = 0.5f;
            constants.Radius = 10.0f;
            constants.FarClipDistance = 1000.0f;
            constants.SampleCount = 8;
            constants.SampleSphere = new Vector4[MaxSampleCount];

            LinearDepthMapSampler = SamplerState.PointClamp;
            NormalMapSampler = SamplerState.PointClamp;

            Enabled = true;

            dirtyFlags = DirtyFlags.Random | DirtyFlags.RandomNormals | DirtyFlags.SampleSphere |
                DirtyFlags.Projection | DirtyFlags.Constants;
        }

        public void Draw(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            Apply(context);

            context.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.Draw(3);
        }

        void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            if ((dirtyFlags & DirtyFlags.Random) != 0)
            {
                random = new Random(seed);

                dirtyFlags &= ~DirtyFlags.Random;
                dirtyFlags |= DirtyFlags.RandomNormals | DirtyFlags.SampleSphere;
            }

            SetRandomNormals(context);
            SetSampleSphere();
            SetProjection();

            if ((dirtyFlags & DirtyFlags.Constants) != 0)
            {
                constantBuffer.SetData(context, constants);

                dirtyFlags &= ~DirtyFlags.Constants;
            }

            context.VertexShaderConstantBuffers[0] = constantBuffer;
            context.VertexShader = sharedDeviceResource.VertexShader;

            context.PixelShaderConstantBuffers[0] = constantBuffer;
            context.PixelShader = sharedDeviceResource.PixelShader;

            context.PixelShaderResources[0] = LinearDepthMap;
            context.PixelShaderResources[1] = NormalMap;
            context.PixelShaderResources[2] = randomNormalMap;
            context.PixelShaderSamplers[0] = LinearDepthMapSampler;
            context.PixelShaderSamplers[1] = NormalMapSampler;
        }

        void SetRandomNormals(DeviceContext context)
        {
            if ((dirtyFlags & DirtyFlags.RandomNormals) != 0)
            {
                if (randomNormalMap != null)
                    randomNormalMap.Dispose();

                var normals = new NormalizedByte4[RandomNormalMapSize * RandomNormalMapSize];
                for (int i = 0; i < normals.Length; i++)
                {
                    var normal = new Vector4
                    {
                        X = (float) random.NextDouble() * 2.0f - 1.0f,
                        Y = (float) random.NextDouble() * 2.0f - 1.0f,
                        Z = (float) random.NextDouble() * 2.0f - 1.0f,
                        W = 0
                    };
                    normal.Normalize();

                    normals[i] = new NormalizedByte4(normal);
                }

                randomNormalMap = context.Device.CreateTexture2D();
                randomNormalMap.Width = RandomNormalMapSize;
                randomNormalMap.Height = RandomNormalMapSize;
                randomNormalMap.Format = SurfaceFormat.NormalizedByte4;
                randomNormalMap.Initialize();
                randomNormalMap.SetData(context, normals);


                dirtyFlags &= ~DirtyFlags.RandomNormals;
            }
        }

        void SetSampleSphere()
        {
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

                    // 球面内のランダム位置とするためのスケール。
                    // 均一ではなく中心に偏った分布とするために、
                    // それらしくスケールを調整。
                    // http://john-chapman-graphics.blogspot.jp/2013/01/ssao-tutorial.html
                    float scale = (float) i / (float) MaxSampleCount;
                    scale = MathHelper.Lerp(0.1f, 1.0f, scale * scale);

                    // 単位球面内のランダム点。
                    Vector3.Multiply(ref vector, scale, out vector);

                    constants.SampleSphere[i] = new Vector4(vector, 0);
                }

                dirtyFlags &= ~DirtyFlags.SampleSphere;
                dirtyFlags |= DirtyFlags.Constants;
            }
        }

        void SetProjection()
        {
            if ((dirtyFlags & DirtyFlags.Projection) != 0)
            {
                constants.FocalLength.X = projection.M11;
                constants.FocalLength.Y = projection.M22;

                float fov;
                float aspectRatio;
                float left;
                float right;
                float bottom;
                float top;
                float nearClipDistance;
                float farClipDistance;
                projection.ExtractPerspective(
                    out fov, out aspectRatio, out left, out right, out bottom, out top, out nearClipDistance, out farClipDistance);

                constants.FarClipDistance = farClipDistance;

                dirtyFlags &= ~DirtyFlags.Projection;
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

        ~SSAOMap()
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
