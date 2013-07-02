#region Using

using System;
using System.Runtime.InteropServices;
using Libra.Graphics.Toolkit.Properties;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class CloudEffect : IEffect, IEffectMatrices, IDisposable
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(Resources.CloudVS);

                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(Resources.CloudPS);
            }
        }

        #endregion

        #region ParametersPerObjectVS

        struct ParametersPerObjectVS
        {
            public Matrix WorldViewProjection;
        }

        #endregion

        #region ParametersPerObjectPS

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        struct ParametersPerObjectPS
        {
            [FieldOffset(0)]
            public Vector3 DiffuseColor;

            [FieldOffset(16)]
            public int LayerCount;

            [FieldOffset(20)]
            public float Density;
        }

        #endregion

        #region ParametersPerFramePS

        [StructLayout(LayoutKind.Sequential, Size = 16 * MaxLayerCount)]
        struct ParametersPerFramePS
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxLayerCount)]
            public Vector4[] Offsets;
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            ConstantBufferPerObjectVS   = (1 << 0),
            ConstantBufferPerObjectPS   = (1 << 1),
            ConstantBufferPerFramePS    = (1 << 2),
            ViewProjection              = (1 << 3),
            WorldViewProjection         = (1 << 4),
            Offsets                     = (1 << 5),
            MaterialColor               = (1 << 6)
        }

        #endregion

        public const int MaxLayerCount = 3;

        SharedDeviceResource sharedDeviceResource;

        ConstantBuffer constantBufferPerObjectVS;

        ConstantBuffer constantBufferPerObjectPS;

        ConstantBuffer constantBufferPerFramePS;

        ParametersPerObjectVS parametersPerObjectVS;

        ParametersPerObjectPS parametersPerObjectPS;

        ParametersPerFramePS parametersPerFramePS;

        Matrix world;

        Matrix view;

        Matrix projection;

        Matrix viewProjection;

        //Vector3 ambientLightColor;

        Vector3 diffuseColor;

        //Vector3 emissiveColor;

        Vector2[] pixelOffsets;

        int textureWidth;

        int textureHeight;

        ShaderResourceView[] volumeMaps;

        DirtyFlags dirtyFlags;

        public DeviceContext DeviceContext { get; private set; }

        public Matrix World
        {
            get { return world; }
            set
            {
                world = value;

                dirtyFlags |= DirtyFlags.WorldViewProjection;
            }
        }

        public Matrix View
        {
            get { return view; }
            set
            {
                view = value;

                dirtyFlags |= DirtyFlags.ViewProjection;
            }
        }

        public Matrix Projection
        {
            get { return projection; }
            set
            {
                projection = value;

                dirtyFlags |= DirtyFlags.ViewProjection;
            }
        }

        //public Vector3 AmbientLightColor
        //{
        //    get { return ambientLightColor; }
        //    set
        //    {
        //        ambientLightColor = value;

        //        dirtyFlags |= DirtyFlags.MaterialColor;
        //    }
        //}

        public Vector3 DiffuseColor
        {
            get { return diffuseColor; }
            set
            {
                diffuseColor = value;

                dirtyFlags |= DirtyFlags.MaterialColor;
            }
        }

        //public Vector3 EmissiveColor
        //{
        //    get { return emissiveColor; }
        //    set
        //    {
        //        emissiveColor = value;

        //        dirtyFlags |= DirtyFlags.MaterialColor;
        //    }
        //}

        //public Vector3 SpecularColor
        //{
        //    get { return parametersPerObjectPS.SpecularColor; }
        //    set
        //    {
        //        parametersPerObjectPS.SpecularColor = value;

        //        dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
        //    }
        //}

        //public float SpecularPower
        //{
        //    get { return parametersPerObjectPS.SpecularPower; }
        //    set
        //    {
        //        parametersPerObjectPS.SpecularPower = value;

        //        dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
        //    }
        //}

        public float Density
        {
            get { return parametersPerObjectPS.Density; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                parametersPerObjectPS.Density = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public int LayerCount
        {
            get { return parametersPerObjectPS.LayerCount; }
            set
            {
                parametersPerObjectPS.LayerCount = value;

                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }
        }

        public Vector2 GetOffset(int index)
        {
            if ((uint) MaxLayerCount <= (uint) index) throw new ArgumentOutOfRangeException("index");

            return pixelOffsets[index];
        }

        public void SetOffset(int index, Vector2 value)
        {
            if ((uint) MaxLayerCount <= (uint) index) throw new ArgumentOutOfRangeException("index");

            pixelOffsets[index] = value;

            dirtyFlags |= DirtyFlags.Offsets;
        }

        public ShaderResourceView GetVolumeMap(int index)
        {
            if ((uint) MaxLayerCount <= (uint) index) throw new ArgumentOutOfRangeException("index");

            return volumeMaps[index];
        }

        public void SetVolumeMap(int index, ShaderResourceView value)
        {
            if ((uint) MaxLayerCount <= (uint) index) throw new ArgumentOutOfRangeException("index");

            volumeMaps[index] = value;
        }

        public SamplerState VolumeMapSampler { get; set; }

        public CloudEffect(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<CloudEffect, SharedDeviceResource>();

            constantBufferPerObjectVS = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObjectVS.Initialize<ParametersPerObjectVS>();
            constantBufferPerObjectPS = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerObjectPS.Initialize<ParametersPerObjectPS>();
            constantBufferPerFramePS = deviceContext.Device.CreateConstantBuffer();
            constantBufferPerFramePS.Initialize<ParametersPerFramePS>();

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;
            viewProjection = Matrix.Identity;
            diffuseColor = Vector3.One;
            pixelOffsets = new Vector2[MaxLayerCount];
            volumeMaps = new ShaderResourceView[MaxLayerCount];
            
            parametersPerObjectVS.WorldViewProjection = Matrix.Identity;

            parametersPerObjectPS.LayerCount = MaxLayerCount;
            parametersPerObjectPS.Density = 1.0f;

            parametersPerFramePS.Offsets = new Vector4[MaxLayerCount];

            dirtyFlags =
                DirtyFlags.ConstantBufferPerObjectVS |
                DirtyFlags.ConstantBufferPerObjectPS |
                DirtyFlags.ConstantBufferPerFramePS |
                DirtyFlags.Offsets |
                DirtyFlags.MaterialColor;
        }

        public void Apply()
        {
            if (volumeMaps[0] != null)
            {
                Texture2D texture2D = volumeMaps[0].Resource as Texture2D;
                if (texture2D != null && (textureWidth != texture2D.Width || textureHeight != texture2D.Height))
                {
                    textureWidth = texture2D.Width;
                    textureHeight = texture2D.Height;

                    dirtyFlags |= DirtyFlags.Offsets;
                }
            }

            if ((dirtyFlags & DirtyFlags.Offsets) != 0)
            {
                for (int i = 0; i < pixelOffsets.Length; i++)
                {
                    parametersPerFramePS.Offsets[i] = new Vector4
                    {
                        X = pixelOffsets[i].X / (float) textureWidth,
                        Y = pixelOffsets[i].Y / (float) textureHeight
                    };
                }

                dirtyFlags &= ~DirtyFlags.Offsets;
                dirtyFlags |= DirtyFlags.ConstantBufferPerFramePS;
            }

            if ((dirtyFlags & DirtyFlags.ViewProjection) != 0)
            {
                Matrix.Multiply(ref view, ref projection, out viewProjection);

                dirtyFlags &= ~DirtyFlags.ViewProjection;
                dirtyFlags |= DirtyFlags.WorldViewProjection;
            }

            if ((dirtyFlags & DirtyFlags.WorldViewProjection) != 0)
            {
                Matrix worldViewProjection;
                Matrix.Multiply(ref world, ref viewProjection, out worldViewProjection);

                Matrix.Transpose(ref worldViewProjection, out parametersPerObjectVS.WorldViewProjection);

                dirtyFlags &= ~DirtyFlags.WorldViewProjection;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectVS;
            }

            if ((dirtyFlags & DirtyFlags.MaterialColor) != 0)
            {
                parametersPerObjectPS.DiffuseColor = diffuseColor;

                //parametersPerObjectPS.EmissiveColor.X = (emissiveColor.X + ambientLightColor.X * diffuseColor.X) * alpha;
                //parametersPerObjectPS.EmissiveColor.Y = (emissiveColor.Y + ambientLightColor.Y * diffuseColor.Y) * alpha;
                //parametersPerObjectPS.EmissiveColor.Z = (emissiveColor.Z + ambientLightColor.Z * diffuseColor.Z) * alpha;

                dirtyFlags &= ~DirtyFlags.MaterialColor;
                dirtyFlags |= DirtyFlags.ConstantBufferPerObjectPS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObjectVS) != 0)
            {
                constantBufferPerObjectVS.SetData(DeviceContext, parametersPerObjectVS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObjectVS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerObjectPS) != 0)
            {
                constantBufferPerObjectPS.SetData(DeviceContext, parametersPerObjectPS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerObjectPS;
            }

            if ((dirtyFlags & DirtyFlags.ConstantBufferPerFramePS) != 0)
            {
                constantBufferPerFramePS.SetData(DeviceContext, parametersPerFramePS);

                dirtyFlags &= ~DirtyFlags.ConstantBufferPerFramePS;
            }

            DeviceContext.VertexShader = sharedDeviceResource.VertexShader;
            DeviceContext.VertexShaderConstantBuffers[0] = constantBufferPerObjectVS;

            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBufferPerObjectPS;
            DeviceContext.PixelShaderConstantBuffers[1] = constantBufferPerFramePS;
            for (int i = 0; i < volumeMaps.Length; i++)
            {
                DeviceContext.PixelShaderResources[i] = volumeMaps[i];
            }
            DeviceContext.PixelShaderSamplers[0] = VolumeMapSampler;
        }

        #region IDisposable

        bool disposed;

        ~CloudEffect()
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
                constantBufferPerObjectVS.Dispose();
                constantBufferPerObjectPS.Dispose();
                constantBufferPerFramePS.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
