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

        #region ParametersPerCamera

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct ParametersPerCamera
        {
            [FieldOffset(0)]
            public Vector2 FocalLength;

            [FieldOffset(8)]
            public float FarClipDistance;
        }

        #endregion

        #region ParametersPerObjectPS

        [StructLayout(LayoutKind.Explicit, Size = 16 + 16 * MaxSampleCount)]
        public struct ParametersPerObjectPS
        {
            [FieldOffset(0)]
            public float Strength;

            [FieldOffset(4)]
            public float Attenuation;

            [FieldOffset(8)]
            public float Radius;

            [FieldOffset(12)]
            public float SampleCount;

            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxSampleCount)]
            public Vector4[] SampleSphere;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerCamera     = (1 << 0),
            ConstantBufferPerObjectPS   = (1 << 1),
            Random                      = (1 << 2),
            RandomNormals               = (1 << 3),
            SampleSphere                = (1 << 4),
            Projection                  = (1 << 5),
        }

        #endregion

        public const int MaxSampleCount = 128;

        const int RandomNormalMapSize = 64;

        SharedDeviceResource sharedDeviceResource;

        // Per-camera は VS/PS 共通。
        ConstantBuffer constantBufferPerCamera;

        ConstantBuffer constantBufferPerObjectPS;

        ParametersPerCamera parametersPerCamera;

        ParametersPerObjectPS parametersPerObjectPS;

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
            get { return parametersPerObjectPS.Strength; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObjectPS.Strength = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public float Attenuation
        {
            get { return parametersPerObjectPS.Attenuation; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObjectPS.Attenuation = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public float Radius
        {
            get { return parametersPerObjectPS.Radius; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObjectPS.Radius = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public int SampleCount
        {
            get { return (int) parametersPerObjectPS.SampleCount; }
            set
            {
                if (value < 1 || MaxSampleCount < value) throw new ArgumentOutOfRangeException("value");

                parametersPerObjectPS.SampleCount = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
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

            constantBufferPerCamera = context.Device.CreateConstantBuffer();
            constantBufferPerCamera.Initialize<ParametersPerCamera>();

            constantBufferPerObjectPS = context.Device.CreateConstantBuffer();
            constantBufferPerObjectPS.Initialize<ParametersPerObjectPS>();

            seed = 0;

            parametersPerCamera.FocalLength = Vector2.One;
            parametersPerCamera.FarClipDistance = 1000.0f;

            parametersPerObjectPS.Strength = 5.0f;
            parametersPerObjectPS.Attenuation = 0.5f;
            parametersPerObjectPS.Radius = 10.0f;
            parametersPerObjectPS.SampleCount = 8;
            parametersPerObjectPS.SampleSphere = new Vector4[MaxSampleCount];

            LinearDepthMapSampler = SamplerState.PointClamp;
            NormalMapSampler = SamplerState.PointClamp;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerCamera |
                DirtyFlags.ConstantBufferPerObjectPS |
                DirtyFlags.Random |
                DirtyFlags.RandomNormals |
                DirtyFlags.SampleSphere |
                DirtyFlags.Projection;
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

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerCamera) != 0)
            {
                constantBufferPerCamera.SetData(Context, parametersPerCamera);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerCamera;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObjectPS) != 0)
            {
                constantBufferPerObjectPS.SetData(Context, parametersPerObjectPS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObjectPS;
            }

            Context.VertexShaderConstantBuffers[0] = constantBufferPerCamera;
            Context.VertexShader = sharedDeviceResource.VertexShader;

            Context.PixelShaderConstantBuffers[0] = constantBufferPerObjectPS;
            Context.PixelShaderConstantBuffers[1] = constantBufferPerCamera;
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

                    parametersPerObjectPS.SampleSphere[i] = new Vector4(vector, 0);
                }

                dirtyFlags &= ~DirtyFlags.SampleSphere;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        void SetProjection()
        {
            if ((dirtyFlags & DirtyFlags.Projection) != 0)
            {
                parametersPerCamera.FocalLength.X = projection.M11;
                parametersPerCamera.FocalLength.Y = projection.M22;
                parametersPerCamera.FarClipDistance = projection.PerspectiveFarClipDistance;

                dirtyFlags &= ~DirtyFlags.Projection;
                dirtyFlags |= DirtyFlags.ConstantBufferPerCamera | DirtyFlags.ConstantBufferPerObjectPS;
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
                constantBufferPerObjectPS.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
