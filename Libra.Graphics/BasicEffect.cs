#region Using

using System;
using Libra.Graphics.Properties;

#endregion

namespace Libra.Graphics
{
    public sealed class BasicEffect : IEffect, IEffectMatrices, IEffectFog
    {
        #region VertexShaderDefinition

        sealed class VertexShaderDefinition
        {
            public byte[] Bytecode;

            public string Name;

            public VertexShaderDefinition(byte[] bytecode, string name)
            {
                Bytecode = bytecode;
                Name = name;
            }
        }

        #endregion

        #region PixelShaderDefinition

        sealed class PixelShaderDefinition
        {
            public byte[] Bytecode;

            public string Name;

            public PixelShaderDefinition(byte[] bytecode, string name)
            {
                Bytecode = bytecode;
                Name = name;
            }
        }

        #endregion

        #region SharedDeviceResource

        sealed class SharedDeviceResource
        {
            Device device;

            VertexShader[] vertexShaders;

            PixelShader[] pixelShaders;

            public SharedDeviceResource(Device device)
            {
                this.device = device;

                vertexShaders = new VertexShader[VertexShaderCount];
                pixelShaders = new PixelShader[PixelShaderCount];
            }

            public VertexShader GetVertexShader(int index)
            {
                lock (this)
                {
                    var vertexShader = vertexShaders[index];

                    if (vertexShader == null)
                    {
                        var definition = VertexShaderDefinitions[index];

                        vertexShader = device.CreateVertexShader();
                        vertexShader.Name = definition.Name;
                        vertexShader.Initialize(definition.Bytecode);

                        vertexShaders[index] = vertexShader;
                    }

                    return vertexShader;
                }
            }

            public PixelShader GetPixelShader(int index)
            {
                lock (this)
                {
                    var pixelShader = pixelShaders[index];

                    if (pixelShader == null)
                    {
                        var definition = PixelShaderDefinitions[index];

                        pixelShader = device.CreatePixelShader();
                        pixelShader.Name = definition.Name;
                        pixelShader.Initialize(PixelShaderDefinitions[index].Bytecode);

                        pixelShaders[index] = pixelShader;
                    }

                    return pixelShader;
                }
            }
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            Contants                = (1 << 0),
            WorldViewProj           = (1 << 1),
            WorldInverseTranspose   = (1 << 2),
            EyePosition             = (1 << 3),
            MaterialColor           = (1 << 4),
            FogVector               = (1 << 5),
            FogEnable               = (1 << 6),
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

        #region DirectionalLight

        public sealed class DirectionalLight
        {
            BasicEffect owner;

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

            internal DirectionalLight(BasicEffect owner, int index)
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

            internal DirectionalLightCollection(BasicEffect owner)
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

        const int VertexShaderInputCount = 8;

        const int VertexShaderCount = 20;

        const int PixelShaderCount = 10;

        const int ShaderPermutationCount = 32;

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

        static readonly VertexShaderDefinition[] VertexShaderDefinitions;

        static readonly PixelShaderDefinition[] PixelShaderDefinitions;

        static readonly int[] VertexShaderIndices =
        {
            0,      // basic
            1,      // no fog
            2,      // vertex color
            3,      // vertex color, no fog
            4,      // texture
            5,      // texture, no fog
            6,      // texture + vertex color
            7,      // texture + vertex color, no fog
    
            8,      // vertex lighting
            8,      // vertex lighting, no fog
            9,      // vertex lighting + vertex color
            9,      // vertex lighting + vertex color, no fog
            10,     // vertex lighting + texture
            10,     // vertex lighting + texture, no fog
            11,     // vertex lighting + texture + vertex color
            11,     // vertex lighting + texture + vertex color, no fog
    
            12,     // one light
            12,     // one light, no fog
            13,     // one light + vertex color
            13,     // one light + vertex color, no fog
            14,     // one light + texture
            14,     // one light + texture, no fog
            15,     // one light + texture + vertex color
            15,     // one light + texture + vertex color, no fog
    
            16,     // pixel lighting
            16,     // pixel lighting, no fog
            17,     // pixel lighting + vertex color
            17,     // pixel lighting + vertex color, no fog
            18,     // pixel lighting + texture
            18,     // pixel lighting + texture, no fog
            19,     // pixel lighting + texture + vertex color
            19,     // pixel lighting + texture + vertex color, no fog
        };

        static readonly int[] PixelShaderIndices =
        {
            0,      // basic
            1,      // no fog
            0,      // vertex color
            1,      // vertex color, no fog
            2,      // texture
            3,      // texture, no fog
            2,      // texture + vertex color
            3,      // texture + vertex color, no fog
    
            4,      // vertex lighting
            5,      // vertex lighting, no fog
            4,      // vertex lighting + vertex color
            5,      // vertex lighting + vertex color, no fog
            6,      // vertex lighting + texture
            7,      // vertex lighting + texture, no fog
            6,      // vertex lighting + texture + vertex color
            7,      // vertex lighting + texture + vertex color, no fog
    
            4,      // one light
            5,      // one light, no fog
            4,      // one light + vertex color
            5,      // one light + vertex color, no fog
            6,      // one light + texture
            7,      // one light + texture, no fog
            6,      // one light + texture + vertex color
            7,      // one light + texture + vertex color, no fog
    
            8,      // pixel lighting
            8,      // pixel lighting, no fog
            8,      // pixel lighting + vertex color
            8,      // pixel lighting + vertex color, no fog
            9,      // pixel lighting + texture
            9,      // pixel lighting + texture, no fog
            9,      // pixel lighting + texture + vertex color
            9,      // pixel lighting + texture + vertex color, no fog
        };

        static BasicEffect()
        {
            VertexShaderDefinitions = new[]
            {
                new VertexShaderDefinition(Resources.BasicEffectVSBasic, "BasicEffect_VSBasic"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicNoFog, "BasicEffect_VSBasicNoFog"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicVc, "BasicEffect_VSBasicVc"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicVcNoFog, "BasicEffect_VSBasicVcNoFog"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicTx, "BasicEffect_VSBasicTx"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicTxNoFog, "BasicEffect_VSBasicTxNoFog"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicTxVc, "BasicEffect_VSBasicTxVc"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicTxVcNoFog, "BasicEffect_VSBasicTxVcNoFog"),

                new VertexShaderDefinition(Resources.BasicEffectVSBasicVertexLighting, "BasicEffect_VSBasicVertexLighting"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicVertexLightingVc, "BasicEffect_VSBasicVertexLightingVc"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicVertexLightingTx, "BasicEffect_VSBasicVertexLightingTx"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicVertexLightingTxVc, "BasicEffect_VSBasicVertexLightingTxVc"),

                new VertexShaderDefinition(Resources.BasicEffectVSBasicOneLight, "BasicEffect_VSBasicOneLight"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicOneLightVc, "BasicEffect_VSBasicOneLightVc"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicOneLightTx, "BasicEffect_VSBasicOneLightTx"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicOneLightTxVc, "BasicEffect_VSBasicOneLightTxVc"),

                new VertexShaderDefinition(Resources.BasicEffectVSBasicPixelLighting, "BasicEffect_VSBasicPixelLighting"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicPixelLightingVc, "BasicEffect_VSBasicPixelLightingVc"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicPixelLightingTx, "BasicEffect_VSBasicPixelLightingTx"),
                new VertexShaderDefinition(Resources.BasicEffectVSBasicPixelLightingTxVc, "BasicEffect_VSBasicPixelLightingTxVc"),
            };

            PixelShaderDefinitions = new[]
            {
                new PixelShaderDefinition(Resources.BasicEffectPSBasic, "BasicEffect_PSBasic"),
                new PixelShaderDefinition(Resources.BasicEffectPSBasicNoFog, "BasicEffect_PSBasicNoFog"),
                new PixelShaderDefinition(Resources.BasicEffectPSBasicTx, "BasicEffect_PSBasicTx"),
                new PixelShaderDefinition(Resources.BasicEffectPSBasicTxNoFog, "BasicEffect_PSBasicTxNoFog"),

                new PixelShaderDefinition(Resources.BasicEffectPSBasicVertexLighting, "BasicEffect_PSBasicVertexLighting"),
                new PixelShaderDefinition(Resources.BasicEffectPSBasicVertexLightingNoFog, "BasicEffect_PSBasicVertexLightingNoFog"),
                new PixelShaderDefinition(Resources.BasicEffectPSBasicVertexLightingTx, "BasicEffect_PSBasicVertexLightingTx"),
                new PixelShaderDefinition(Resources.BasicEffectPSBasicVertexLightingTxNoFog, "BasicEffect_PSBasicVertexLightingTxNoFog"),

                new PixelShaderDefinition(Resources.BasicEffectPSBasicPixelLighting, "BasicEffect_PSBasicPixelLighting"),
                new PixelShaderDefinition(Resources.BasicEffectPSBasicPixelLightingTx, "BasicEffect_PSBasicPixelLightingTx"),
            };
        }

        SharedDeviceResource sharedDeviceResource;

        DirtyFlags dirtyFlags;

        Constants constants;

        Matrix world;

        Matrix view;

        Matrix projection;

        Vector3 diffuseColor;

        Vector3 emissiveColor;

        float alpha;

        bool lightingEnabled;

        Vector3 ambientLightColor;

        DirectionalLightCollection directionalLights;

        bool fogEnabled;

        float fogStart;

        float fogEnd;

        // 内部作業用
        Matrix worldView;

        ConstantBuffer constantBuffer;

        public DeviceContext DeviceContext { get; private set; }

        public Matrix World
        {
            get { return world; }
            set
            {
                world = value;
                dirtyFlags |= DirtyFlags.WorldViewProj | DirtyFlags.WorldInverseTranspose | DirtyFlags.FogVector;
            }
        }

        public Matrix View
        {
            get { return view; }
            set
            {
                view = value;
                dirtyFlags |= DirtyFlags.WorldViewProj | DirtyFlags.EyePosition | DirtyFlags.FogVector;
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

        public bool PerPixelLighting { get; set; }

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

        public bool FogEnabled
        {
            get { return fogEnabled; }
            set
            {
                fogEnabled = value;
                dirtyFlags |= DirtyFlags.FogEnable;
            }
        }

        public float FogStart
        {
            get { return fogStart; }
            set
            {
                fogStart = value;
                dirtyFlags |= DirtyFlags.FogVector;
            }
        }

        public float FogEnd
        {
            get { return fogEnd; }
            set
            {
                fogEnd = value;
                dirtyFlags |= DirtyFlags.FogVector;
            }
        }

        public Vector3 FogColor
        {
            get { return constants.FogColor.ToVector3(); }
            set
            {
                constants.FogColor = value.ToVector4();
                dirtyFlags |= DirtyFlags.Contants;
            }
        }

        public bool VertexColorEnabled { get; set; }

        public bool TextureEnabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public BasicEffect(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<BasicEffect, SharedDeviceResource>();

            directionalLights = new DirectionalLightCollection(this);

            constantBuffer = deviceContext.Device.CreateConstantBuffer();
            constantBuffer.Usage = ResourceUsage.Dynamic;
            constantBuffer.Initialize<Constants>();

            // デフォルト値。
            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;

            diffuseColor = Vector3.One;
            alpha = 1.0f;

            fogEnabled = false;
            fogStart = 0;
            fogEnd = 1;

            for (int i = 0; i < DirectionalLightCount; i++)
            {
                directionalLights[i].Enabled = (i == 0);
                directionalLights[i].Direction = new Vector3(0, -1, 0);
                directionalLights[i].DiffuseColor = Vector3.One;
                directionalLights[i].SpecularColor = Vector3.Zero;
            }

            constants.SpecularColorPower = new Vector4(1, 1, 1, 16);

            dirtyFlags = DirtyFlags.Contants |
                DirtyFlags.WorldViewProj |
                DirtyFlags.WorldInverseTranspose |
                DirtyFlags.EyePosition |
                DirtyFlags.MaterialColor |
                DirtyFlags.FogVector |
                DirtyFlags.FogEnable;
        }

        public void Apply()
        {
            SetWorldViewProjConstant();
            SetFogConstants();
            SetLightConstants();

            if (TextureEnabled)
            {
                DeviceContext.PixelShaderResources[0] = Texture;
            }

            ApplyShaders(GetCurrentShaderPermutation());
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

                // シェーダは列優先。
                Matrix.Transpose(ref worldViewProj, out constants.WorldViewProj);

                dirtyFlags &= ~DirtyFlags.WorldViewProj;
                dirtyFlags |= DirtyFlags.Contants;
            }
        }

        void SetFogConstants()
        {
            if (fogEnabled)
            {
                if ((dirtyFlags & (DirtyFlags.FogVector | DirtyFlags.FogEnable)) != 0)
                {
                    if (fogStart == fogEnd)
                    {
                        constants.FogVector = new Vector4(0, 0, 0, 1);
                    }
                    else
                    {
                        float scale = 1f / (fogStart - fogEnd);

                        constants.FogVector.X = worldView.M13 * scale;
                        constants.FogVector.Y = worldView.M32 * scale;
                        constants.FogVector.Z = worldView.M33 * scale;
                        constants.FogVector.W = (worldView.M43 + fogStart) * scale;
                    }

                    dirtyFlags &= ~(DirtyFlags.FogVector | DirtyFlags.FogEnable);
                    dirtyFlags |= DirtyFlags.Contants;
                }
            }
            else
            {
                if ((dirtyFlags & DirtyFlags.FogEnable) != 0)
                {
                    constants.FogVector = Vector4.Zero;
                    dirtyFlags &= ~DirtyFlags.FogEnable;
                    dirtyFlags |= DirtyFlags.Contants;
                }
            }
        }

        void SetLightConstants()
        {
            if (lightingEnabled)
            {
                if ((dirtyFlags & DirtyFlags.WorldInverseTranspose) != 0)
                {
                    // シェーダは列優先。
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

        int GetCurrentShaderPermutation()
        {
            int permutation = 0;

            if (!fogEnabled)
            {
                permutation += 1;
            }

            if (VertexColorEnabled)
            {
                permutation += 2;
            }

            if (TextureEnabled)
            {
                permutation += 4;
            }

            if (lightingEnabled)
            {
                if (PerPixelLighting)
                {
                    permutation += 24;
                }
                else if (!directionalLights[1].Enabled && !directionalLights[2].Enabled)
                {
                    permutation += 16;
                }
                else
                {
                    permutation += 8;
                }
            }

            return permutation;
        }

        void ApplyShaders(int permutation)
        {
            var vertexShaderIndex = VertexShaderIndices[permutation];
            var pixelShaderIndex = PixelShaderIndices[permutation];

            var vertexShader = sharedDeviceResource.GetVertexShader(vertexShaderIndex);
            var pixelShader = sharedDeviceResource.GetPixelShader(pixelShaderIndex);

            DeviceContext.VertexShader = vertexShader;
            DeviceContext.PixelShader = pixelShader;

            if ((dirtyFlags & DirtyFlags.Contants) != 0)
            {
                constantBuffer.SetData(DeviceContext, constants);
                dirtyFlags &= ~DirtyFlags.Contants;
            }

            DeviceContext.VertexShaderConstantBuffers[0] = constantBuffer;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBuffer;
        }

        #region IDisposable

        bool disposed;

        ~BasicEffect()
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
