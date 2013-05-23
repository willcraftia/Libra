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

        #region ConstantsVS

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct ConstantsVS
        {
            public Vector2 FocalLength;
        }

        #endregion

        #region ConstantsPS

        [StructLayout(LayoutKind.Explicit, Size = 32 + 16 * MaxSampleCount)]
        public struct ConstantsPS
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
            ConstantsVS     = (1 << 4),
            ConstantsPS     = (1 << 5)
        }

        #endregion

        public const int MaxSampleCount = 128;

        const int RandomNormalMapSize = 64;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferVS;

        ConstantBuffer constantBufferPS;

        ConstantsVS constantsVS;

        ConstantsPS constantsPS;

        int seed;

        Random random;

        Texture2D randomNormalMap;
        
        Matrix projection;

        DirtyFlags dirtyFlags;

        public DeviceContext Context { get; private set; }

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
            get { return constantsPS.Strength; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constantsPS.Strength = value;

                dirtyFlags |= DirtyFlags.ConstantsPS;
            }
        }

        public float Attenuation
        {
            get { return constantsPS.Attenuation; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                constantsPS.Attenuation = value;

                dirtyFlags |= DirtyFlags.ConstantsPS;
            }
        }

        public float Radius
        {
            get { return constantsPS.Radius; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                constantsPS.Radius = value;

                dirtyFlags |= DirtyFlags.ConstantsPS;
            }
        }

        public int SampleCount
        {
            get { return (int) constantsPS.SampleCount; }
            set
            {
                if (value < 1 || MaxSampleCount < value) throw new ArgumentOutOfRangeException("value");

                constantsPS.SampleCount = value;

                dirtyFlags |= DirtyFlags.ConstantsPS;
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

        public SSAOMap(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            Context = context;

            sharedDeviceResource = context.Device.GetSharedResource<SSAOMap, SharedDeviceResource>();

            constantBufferVS = context.Device.CreateConstantBuffer();
            constantBufferVS.Initialize<ConstantsVS>();

            constantBufferPS = context.Device.CreateConstantBuffer();
            constantBufferPS.Initialize<ConstantsPS>();

            seed = 0;

            constantsVS.FocalLength = Vector2.One;

            constantsPS.FocalLength = Vector2.One;
            constantsPS.Strength = 5.0f;
            constantsPS.Attenuation = 0.5f;
            constantsPS.Radius = 10.0f;
            constantsPS.FarClipDistance = 1000.0f;
            constantsPS.SampleCount = 8;
            constantsPS.SampleSphere = new Vector4[MaxSampleCount];

            LinearDepthMapSampler = SamplerState.PointClamp;
            NormalMapSampler = SamplerState.PointClamp;

            Enabled = true;

            dirtyFlags = DirtyFlags.Random | DirtyFlags.RandomNormals | DirtyFlags.SampleSphere |
                DirtyFlags.Projection | DirtyFlags.ConstantsVS | DirtyFlags.ConstantsPS;
        }

        public void Draw()
        {
            Apply();

            Context.PrimitiveTopology = PrimitiveTopology.TriangleList;
            Context.Draw(3);
        }

        void Apply()
        {
            if ((dirtyFlags & DirtyFlags.Random) != 0)
            {
                random = new Random(seed);

                dirtyFlags &= ~DirtyFlags.Random;
                dirtyFlags |= DirtyFlags.RandomNormals | DirtyFlags.SampleSphere;
            }

            SetRandomNormals();
            SetSampleSphere();
            SetProjection();

            if ((dirtyFlags & DirtyFlags.ConstantsVS) != 0)
            {
                constantBufferVS.SetData(Context, constantsVS);

                dirtyFlags &= ~DirtyFlags.ConstantsVS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantsPS) != 0)
            {
                constantBufferPS.SetData(Context, constantsPS);

                dirtyFlags &= ~DirtyFlags.ConstantsPS;
            }

            Context.VertexShaderConstantBuffers[0] = constantBufferVS;
            Context.VertexShader = sharedDeviceResource.VertexShader;

            Context.PixelShaderConstantBuffers[0] = constantBufferPS;
            Context.PixelShader = sharedDeviceResource.PixelShader;

            Context.PixelShaderResources[0] = LinearDepthMap;
            Context.PixelShaderResources[1] = NormalMap;
            Context.PixelShaderResources[2] = randomNormalMap;
            Context.PixelShaderSamplers[0] = LinearDepthMapSampler;
            Context.PixelShaderSamplers[1] = NormalMapSampler;
        }

        void SetRandomNormals()
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

                randomNormalMap = Context.Device.CreateTexture2D();
                randomNormalMap.Width = RandomNormalMapSize;
                randomNormalMap.Height = RandomNormalMapSize;
                randomNormalMap.Format = SurfaceFormat.NormalizedByte4;
                randomNormalMap.Initialize();
                randomNormalMap.SetData(Context, normals);


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

                    constantsPS.SampleSphere[i] = new Vector4(vector, 0);
                }

                dirtyFlags &= ~DirtyFlags.SampleSphere;
                dirtyFlags |= DirtyFlags.ConstantsPS;
            }
        }

        void SetProjection()
        {
            if ((dirtyFlags & DirtyFlags.Projection) != 0)
            {
                constantsVS.FocalLength.X = projection.M11;
                constantsVS.FocalLength.Y = projection.M22;
                constantsPS.FocalLength.X = projection.M11;
                constantsPS.FocalLength.Y = projection.M22;
                constantsPS.FarClipDistance = projection.PerspectiveFarClipDistance;

                dirtyFlags &= ~DirtyFlags.Projection;
                dirtyFlags |= DirtyFlags.ConstantsVS | DirtyFlags.ConstantsPS;
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
                constantBufferPS.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
