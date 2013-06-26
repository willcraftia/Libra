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
            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
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

        #region ParametersPerObject

        [StructLayout(LayoutKind.Explicit, Size = 16 + 16 * MaxSampleCount)]
        public struct ParametersPerObject
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
            ConstantBufferPerCamera = (1 << 0),
            ConstantBufferPerObject = (1 << 1),
            Random                  = (1 << 2),
            RandomNormals           = (1 << 3),
            SampleSphere            = (1 << 4)
        }

        #endregion

        public const int MaxSampleCount = 128;

        const int RandomNormalMapSize = 64;

        SharedDeviceResource sharedDeviceResource;

        FullScreenQuad fullScreenQuad;

        ConstantBuffer constantBufferPerObject;

        ConstantBuffer constantBufferPerCamera;

        ParametersPerObject parametersPerObject;

        ParametersPerCamera parametersPerCamera;

        int seed;

        Random random;

        Texture2D randomNormalMap;
        
        Matrix projection;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

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
            get { return parametersPerObject.Strength; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.Strength = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float Attenuation
        {
            get { return parametersPerObject.Attenuation; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.Attenuation = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public float Radius
        {
            get { return parametersPerObject.Radius; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.Radius = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public int SampleCount
        {
            get { return (int) parametersPerObject.SampleCount; }
            set
            {
                if (value < 1 || MaxSampleCount < value) throw new ArgumentOutOfRangeException("value");

                parametersPerObject.SampleCount = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
            }
        }

        public Matrix Projection
        {
            get { return projection; }
            set
            {
                projection = value;
                fullScreenQuad.Projection = projection;
                parametersPerCamera.FocalLength.X = projection.M11;
                parametersPerCamera.FocalLength.Y = projection.M22;
                parametersPerCamera.FarClipDistance = projection.PerspectiveFarClipDistance;

                dirtyFlags |= DirtyFlags.ConstantBufferPerCamera;
            }
        }

        public ShaderResourceView LinearDepthMap { get; set; }

        public ShaderResourceView NormalMap { get; set; }

        public SamplerState LinearDepthMapSampler { get; set; }

        public SamplerState NormalMapSampler { get; set; }

        public bool Enabled { get; set; }

        public SSAOMap(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<SSAOMap, SharedDeviceResource>();

            fullScreenQuad = new FullScreenQuad(deviceContext);
            fullScreenQuad.ViewRayEnabled = true;

            constantBufferPerObject = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObject.Initialize<ParametersPerObject>();

            constantBufferPerCamera = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerCamera.Initialize<ParametersPerCamera>();

            seed = 0;

            parametersPerCamera.FocalLength = Vector2.One;
            parametersPerCamera.FarClipDistance = 1000.0f;

            parametersPerObject.Strength = 5.0f;
            parametersPerObject.Attenuation = 0.5f;
            parametersPerObject.Radius = 10.0f;
            parametersPerObject.SampleCount = 8;
            parametersPerObject.SampleSphere = new Vector4[MaxSampleCount];

            LinearDepthMapSampler = SamplerState.PointClamp;
            NormalMapSampler = SamplerState.PointClamp;

            Enabled = true;

            dirtyFlags =
                DirtyFlags.ConstantBufferPerCamera |
                DirtyFlags.ConstantBufferPerObject |
                DirtyFlags.Random |
                DirtyFlags.RandomNormals |
                DirtyFlags.SampleSphere;
        }

        public void Draw()
        {
            Apply();

            fullScreenQuad.Draw();
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

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObject) != 0)
            {
                constantBufferPerObject.SetData(DeviceContext, parametersPerObject);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObject;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerCamera) != 0)
            {
                constantBufferPerCamera.SetData(DeviceContext, parametersPerCamera);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerCamera;
            }

            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerObject;
            DeviceContext.PixelShaderConstantBuffers[1] = constantBufferPerCamera;
            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;

            DeviceContext.PixelShaderResources[0] = LinearDepthMap;
            DeviceContext.PixelShaderResources[1] = NormalMap;
            DeviceContext.PixelShaderResources[2] = randomNormalMap;
            DeviceContext.PixelShaderSamplers[0] = LinearDepthMapSampler;
            DeviceContext.PixelShaderSamplers[1] = NormalMapSampler;
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

                randomNormalMap = DeviceContext.Device.CreateTexture2D();
                randomNormalMap.Width = RandomNormalMapSize;
                randomNormalMap.Height = RandomNormalMapSize;
                randomNormalMap.Format = SurfaceFormat.NormalizedByte4;
                randomNormalMap.Initialize();
                randomNormalMap.SetData(DeviceContext, normals);


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

                    parametersPerObject.SampleSphere[i] = new Vector4(vector, 0);
                }

                dirtyFlags &= ~DirtyFlags.SampleSphere;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObject;
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
                constantBufferPerObject.Dispose();
                constantBufferPerCamera.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
