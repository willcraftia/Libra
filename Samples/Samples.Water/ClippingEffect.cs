#region Using

using System;
using System.Runtime.InteropServices;
using Libra;
using Libra.Graphics;
using Libra.Graphics.Compiler;

#endregion

namespace Samples.Water
{
    public sealed class ClippingEffect : IEffect, IEffectMatrices
    {
        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            public VertexShader VertexShader { get; private set; }

            public PixelShader PixelShader { get; private set; }

            public SharedDeviceResource(Device device)
            {
                var compiler = ShaderCompiler.CreateShaderCompiler();
                compiler.RootPath = "Shaders";
                compiler.EnableStrictness = true;
                compiler.OptimizationLevel = OptimizationLevels.Level3;
                //compiler.WarningsAreErrors = true;

                var vsBytecode = compiler.CompileVertexShader("ClippingEffect.hlsl");
                var psBytecode = compiler.CompilePixelShader("ClippingEffect.hlsl");

                VertexShader = device.CreateVertexShader();
                VertexShader.Initialize(vsBytecode);

                PixelShader = device.CreatePixelShader();
                PixelShader.Initialize(psBytecode);
            }
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            Contants                = (1 << 0),
            ClippingContants        = (1 << 1),
            WorldViewProj           = (1 << 2),
            WorldInverseTranspose   = (1 << 3),
            EyePosition             = (1 << 4),
            MaterialColor           = (1 << 5),
        }

        #endregion

        #region Constants

        struct Constants
        {
            // http://msdn.microsoft.com/en-us/library/ff476182(v=vs.85).aspx
            // 定数バッファに限っては、D3D10 以降は 16 バイトの倍数サイズでなければならない。
            // StructLayout や FieldOffset を駆使しても、
            // 最終的には StructLayout で Size を明示しないと保証できない。
            // 全フィールドを 16 バイト基準の型 (Vector4 や Matrix) に揃えると、
            // 単純に 16 バイトの倍数サイズが確定する。
            // これは、Direct Math で XMVECTOR や XMMATRIX に揃える事と同じである。

            public Vector4 DiffuseColor;

            public Vector4 EmissiveColor;

            public Vector4 SpecularColorPower;

            public Vector4 LightDirection0;
            
            public Vector4 LightDirection1;
            
            public Vector4 LightDirection2;

            public Vector4 LightDiffuseColor0;

            public Vector4 LightDiffuseColor1;

            public Vector4 LightDiffuseColor2;

            public Vector4 LightSpecularColor0;

            public Vector4 LightSpecularColor1;

            public Vector4 LightSpecularColor2;

            public Vector4 EyePosition;

            public Vector4 FogColor;

            public Vector4 FogVector;

            public Matrix World;

            public Vector4 WorldInverseTranspose0;

            public Vector4 WorldInverseTranspose1;
            
            public Vector4 WorldInverseTranspose2;

            public Matrix WorldViewProj;

            public Vector3 GetLightDirection(int index)
            {
                if (index == 0)         return LightDirection0.ToVector3();
                else if (index == 1)    return LightDirection1.ToVector3();
                else if (index == 2)    return LightDirection2.ToVector3();
                else throw new ArgumentOutOfRangeException("index");
            }

            public void SetLightDirection(int index, Vector3 value)
            {
                if (index == 0)         LightDirection0 = value.ToVector4();
                else if (index == 1)    LightDirection1 = value.ToVector4();
                else if (index == 2)    LightDirection2 = value.ToVector4();
                else throw new ArgumentOutOfRangeException("index");
            }

            public Vector3 GetLightDiffuseColor(int index)
            {
                if (index == 0)         return LightDiffuseColor0.ToVector3();
                else if (index == 1)    return LightDiffuseColor1.ToVector3();
                else if (index == 2)    return LightDiffuseColor2.ToVector3();
                else throw new ArgumentOutOfRangeException("index");
            }

            public void SetLightDiffuseColor(int index, Vector3 value)
            {
                if (index == 0)         LightDiffuseColor0 = value.ToVector4();
                else if (index == 1)    LightDiffuseColor1 = value.ToVector4();
                else if (index == 2)    LightDiffuseColor2 = value.ToVector4();
                else throw new ArgumentOutOfRangeException("index");
            }

            public Vector3 GetLightSpecularColor(int index)
            {
                if (index == 0)         return LightSpecularColor0.ToVector3();
                else if (index == 1)    return LightSpecularColor1.ToVector3();
                else if (index == 2)    return LightSpecularColor2.ToVector3();
                else throw new ArgumentOutOfRangeException("index");
            }

            public void SetLightSpecularColor(int index, Vector3 value)
            {
                if (index == 0)         LightSpecularColor0 = value.ToVector4();
                else if (index == 1)    LightSpecularColor1 = value.ToVector4();
                else if (index == 2)    LightSpecularColor2 = value.ToVector4();
                else throw new ArgumentOutOfRangeException("index");
            }
        }

        #endregion

        [StructLayout(LayoutKind.Explicit, Size = 16 + 16 * MaxClipPlaneCount)]
        struct ClippingConstants
        {
            [FieldOffset(0)]
            public bool ClippingEnabled;

            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxClipPlaneCount)]
            public Vector4[] ClipPlanes;
        }

        #region DirectionalLight

        public sealed class DirectionalLight
        {
            ClippingEffect owner;

            int index;

            bool enabled;

            Vector3 diffuseColor;

            Vector3 specularColor;

            public bool Enabled
            {
                get { return enabled; }
                set
                {
                    if (enabled == value) return;

                    enabled = value;

                    if (enabled)
                    {
                        owner.constants.SetLightDiffuseColor(index, diffuseColor);
                        owner.constants.SetLightSpecularColor(index, specularColor);
                    }
                    else
                    {
                        owner.constants.SetLightDiffuseColor(index, Vector3.Zero);
                        owner.constants.SetLightSpecularColor(index, Vector3.Zero);
                    }

                    owner.dirtyFlags |= DirtyFlags.Contants;
                }
            }

            public Vector3 Direction
            {
                get { return owner.constants.GetLightDirection(index); }
                set
                {
                    owner.constants.SetLightDirection(index, value);
                    owner.dirtyFlags |= DirtyFlags.Contants;
                }
            }

            public Vector3 DiffuseColor
            {
                get { return diffuseColor; }
                set
                {
                    diffuseColor = value;

                    if (enabled)
                    {
                        owner.constants.SetLightDiffuseColor(index, value);
                        owner.dirtyFlags |= DirtyFlags.Contants;
                    }
                }
            }

            public Vector3 SpecularColor
            {
                get { return specularColor; }
                set
                {
                    specularColor = value;

                    if (enabled)
                    {
                        owner.constants.SetLightSpecularColor(index, value);
                        owner.dirtyFlags |= DirtyFlags.Contants;
                    }
                }
            }

            internal DirectionalLight(ClippingEffect owner, int index)
            {
                this.owner = owner;
                this.index = index;
            }
        }

        #endregion

        #region DirectionalLightCollection

        public sealed class DirectionalLightCollection
        {
            DirectionalLight[] directionalLight;

            public DirectionalLight this[int index]
            {
                get
                {
                    if ((uint) DirectionalLightCount <= (uint) index) throw new ArgumentOutOfRangeException("index");
                    return directionalLight[index];
                }
            }

            internal DirectionalLightCollection(ClippingEffect owner)
            {
                directionalLight = new DirectionalLight[DirectionalLightCount];
                for (int i = 0; i < DirectionalLightCount; i++)
                {
                    directionalLight[i] = new DirectionalLight(owner, i);
                }
            }
        }

        #endregion

        public const int DirectionalLightCount = 3;

        public const int MaxClipPlaneCount = 3;

        static readonly Vector3[] DefaultDirectionalLightDirections =
        {
            new Vector3(-0.5265408f, -0.5735765f, -0.6275069f),
            new Vector3( 0.7198464f,  0.3420201f,  0.6040227f),
            new Vector3( 0.4545195f, -0.7660444f,  0.4545195f),
        };

        static readonly Vector3[] DefaultDirectionalLightDiffuseColors =
        {
            new Vector3(1.0000000f, 0.9607844f, 0.8078432f),
            new Vector3(0.9647059f, 0.7607844f, 0.4078432f),
            new Vector3(0.3231373f, 0.3607844f, 0.3937255f),
        };

        static readonly Vector3[] DefaultDirectionalLightSpecularColors =
        {
            new Vector3(1.0000000f, 0.9607844f, 0.8078432f),
            new Vector3(0.0000000f, 0.0000000f, 0.0000000f),
            new Vector3(0.3231373f, 0.3607844f, 0.3937255f),
        };

        static readonly Vector3 DefaultAmbientLightColor = new Vector3(0.05333332f, 0.09882354f, 0.1819608f);

        SharedDeviceResource sharedDeviceResource;

        DirtyFlags dirtyFlags;

        Constants constants;

        ClippingConstants clippingConstants;

        Matrix world;

        Matrix view;

        Matrix projection;

        Vector3 diffuseColor;

        Vector3 emissiveColor;

        float alpha;

        bool lightingEnabled;

        Vector3 ambientLightColor;

        DirectionalLightCollection directionalLights;

        // 内部作業用
        Matrix worldView;

        ConstantBuffer constantBuffer;

        ConstantBuffer clippingConstantBuffer;

        public DeviceContext DeviceContext { get; private set; }

        public Matrix World
        {
            get { return world; }
            set
            {
                world = value;
                dirtyFlags |= DirtyFlags.WorldViewProj | DirtyFlags.WorldInverseTranspose;
            }
        }

        public Matrix View
        {
            get { return view; }
            set
            {
                view = value;
                dirtyFlags |= DirtyFlags.WorldViewProj | DirtyFlags.EyePosition;
            }
        }

        public Matrix Projection
        {
            get { return projection; }
            set
            {
                projection = value;
                dirtyFlags |= DirtyFlags.WorldViewProj;
            }
        }

        public Vector3 DiffuseColor
        {
            get { return diffuseColor; }
            set
            {
                diffuseColor = value;
                dirtyFlags |= DirtyFlags.MaterialColor;
            }
        }

        public Vector3 EmissiveColor
        {
            get { return emissiveColor; }
            set
            {
                emissiveColor = value;
                dirtyFlags |= DirtyFlags.MaterialColor;
            }
        }

        public Vector3 SpecularColor
        {
            get
            {
                return new Vector3(constants.SpecularColorPower.X,
                                   constants.SpecularColorPower.Y,
                                   constants.SpecularColorPower.Z);
            }
            set
            {
                constants.SpecularColorPower.X = value.X;
                constants.SpecularColorPower.Y = value.Y;
                constants.SpecularColorPower.Z = value.Z;
                dirtyFlags |= DirtyFlags.Contants;
            }
        }

        public float SpecularPower
        {
            get { return constants.SpecularColorPower.W; }
            set
            {
                constants.SpecularColorPower.W = value;
                dirtyFlags |= DirtyFlags.Contants;
            }
        }

        public float Alpha
        {
            get { return alpha; }
            set
            {
                alpha = value;
                dirtyFlags |= DirtyFlags.MaterialColor;
            }
        }

        public bool LightingEnabled
        {
            get { return lightingEnabled; }
            set
            {
                lightingEnabled = value;
                dirtyFlags |= DirtyFlags.MaterialColor;
            }
        }

        public Vector3 AmbientLightColor
        {
            get { return ambientLightColor; }
            set
            {
                ambientLightColor = value;
                dirtyFlags |= DirtyFlags.MaterialColor;
            }
        }

        public DirectionalLightCollection DirectionalLights
        {
            get { return directionalLights; }
        }

        public bool ClippingEnabled
        {
            get { return clippingConstants.ClippingEnabled; }
            set
            {
                clippingConstants.ClippingEnabled = value;

                dirtyFlags |= DirtyFlags.ClippingContants;
            }
        }

        public Vector4 ClipPlane0
        {
            get { return clippingConstants.ClipPlanes[0]; }
            set
            {
                clippingConstants.ClipPlanes[0] = value;

                dirtyFlags |= DirtyFlags.ClippingContants;
            }
        }

        public ClippingEffect(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<ClippingEffect, SharedDeviceResource>();

            directionalLights = new DirectionalLightCollection(this);

            constantBuffer = deviceContext.Device.CreateConstantBuffer();
            constantBuffer.Usage = ResourceUsage.Dynamic;
            constantBuffer.Initialize<Constants>();

            clippingConstantBuffer = deviceContext.Device.CreateConstantBuffer();
            clippingConstantBuffer.Usage = ResourceUsage.Dynamic;
            clippingConstantBuffer.Initialize<ClippingConstants>();

            // デフォルト値。
            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;

            diffuseColor = Vector3.One;
            alpha = 1.0f;

            for (int i = 0; i < DirectionalLightCount; i++)
            {
                directionalLights[i].Enabled = (i == 0);
                directionalLights[i].Direction = new Vector3(0, -1, 0);
                directionalLights[i].DiffuseColor = Vector3.One;
                directionalLights[i].SpecularColor = Vector3.Zero;
            }

            constants.SpecularColorPower = new Vector4(1, 1, 1, 16);

            clippingConstants.ClippingEnabled = true;
            clippingConstants.ClipPlanes = new Vector4[MaxClipPlaneCount];

            dirtyFlags =
                DirtyFlags.Contants |
                DirtyFlags.ClippingContants |
                DirtyFlags.WorldViewProj |
                DirtyFlags.WorldInverseTranspose |
                DirtyFlags.EyePosition |
                DirtyFlags.MaterialColor;
        }

        public void Apply()
        {
            SetWorldViewProjConstant();
            SetLightConstants();

            ApplyShaders();
        }

        public void EnableDefaultLighting()
        {
            LightingEnabled = true;
            AmbientLightColor = DefaultAmbientLightColor;

            for (int i = 0; i < DirectionalLightCount; i++)
            {
                var directionalLight = DirectionalLights[i];
                directionalLight.Enabled = true;
                directionalLight.Direction = DefaultDirectionalLightDirections[i];
                directionalLight.DiffuseColor = DefaultDirectionalLightDiffuseColors[i];
                directionalLight.SpecularColor = DefaultDirectionalLightSpecularColors[i];
            }
        }

        void SetWorldViewProjConstant()
        {
            if ((dirtyFlags & DirtyFlags.WorldViewProj) != 0)
            {
                Matrix.Multiply(ref world, ref view, out worldView);

                Matrix worldViewProj;
                Matrix.Multiply(ref worldView, ref projection, out worldViewProj);

                Matrix.Transpose(ref worldViewProj, out constants.WorldViewProj);

                dirtyFlags &= ~DirtyFlags.WorldViewProj;
                dirtyFlags |= DirtyFlags.Contants;
            }
        }

        void SetLightConstants()
        {
            if (lightingEnabled)
            {
                if ((dirtyFlags & DirtyFlags.WorldInverseTranspose) != 0)
                {
                    Matrix.Transpose(ref world, out constants.World);

                    Matrix worldInverse;
                    Matrix.Invert(ref world, out worldInverse);

                    constants.WorldInverseTranspose0 = new Vector4(worldInverse.M11, worldInverse.M12, worldInverse.M13, 0);
                    constants.WorldInverseTranspose1 = new Vector4(worldInverse.M21, worldInverse.M22, worldInverse.M23, 0);
                    constants.WorldInverseTranspose2 = new Vector4(worldInverse.M31, worldInverse.M32, worldInverse.M33, 0);

                    dirtyFlags &= ~DirtyFlags.WorldInverseTranspose;
                    dirtyFlags |= DirtyFlags.Contants;
                }

                if ((dirtyFlags & DirtyFlags.EyePosition) != 0)
                {
                    Matrix viewInverse;
                    Matrix.Invert(ref view, out viewInverse);

                    constants.EyePosition = viewInverse.Translation.ToVector4();

                    dirtyFlags &= ~DirtyFlags.EyePosition;
                    dirtyFlags |= DirtyFlags.Contants;
                }
            }

            if ((dirtyFlags & DirtyFlags.MaterialColor) != 0)
            {
                if (lightingEnabled)
                {
                    constants.DiffuseColor.X = diffuseColor.X * alpha;
                    constants.DiffuseColor.Y = diffuseColor.Y * alpha;
                    constants.DiffuseColor.Z = diffuseColor.Z * alpha;
                    constants.DiffuseColor.W = alpha;

                    constants.EmissiveColor.X = (emissiveColor.X + ambientLightColor.X * diffuseColor.X) * alpha;
                    constants.EmissiveColor.Y = (emissiveColor.Y + ambientLightColor.Y * diffuseColor.Y) * alpha;
                    constants.EmissiveColor.Z = (emissiveColor.Z + ambientLightColor.Z * diffuseColor.Z) * alpha;
                }
                else
                {
                    constants.DiffuseColor.X = (diffuseColor.X + emissiveColor.X) * alpha;
                    constants.DiffuseColor.Y = (diffuseColor.Y + emissiveColor.Y) * alpha;
                    constants.DiffuseColor.Z = (diffuseColor.Z + emissiveColor.Z) * alpha;
                    constants.DiffuseColor.W = alpha;
                }

                dirtyFlags &= ~DirtyFlags.MaterialColor;
                dirtyFlags |= DirtyFlags.Contants;
            }
        }

        void ApplyShaders()
        {
            DeviceContext.VertexShader = sharedDeviceResource.VertexShader;
            DeviceContext.PixelShader = sharedDeviceResource.PixelShader;

            if ((dirtyFlags & DirtyFlags.Contants) != 0)
            {
                DeviceContext.SetData(constantBuffer, constants);
                dirtyFlags &= ~DirtyFlags.Contants;
            }

            if ((dirtyFlags & DirtyFlags.ClippingContants) != 0)
            {
                DeviceContext.SetData(clippingConstantBuffer, clippingConstants);
                dirtyFlags &= ~DirtyFlags.ClippingContants;
            }

            DeviceContext.VertexShaderConstantBuffers[0] = constantBuffer;
            DeviceContext.VertexShaderConstantBuffers[1] = clippingConstantBuffer;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBuffer;
        }

        #region IDisposable

        bool disposed;

        ~ClippingEffect()
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
                constantBuffer.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
