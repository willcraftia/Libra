#region Using

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    public sealed partial class BasicEffect : IEffect, IEffectMatrices
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

        #region Resources

        sealed class Resources
        {
            IDevice device;

            InputLayout[] inputLayouts;

            VertexShader[] vertexShaders;

            PixelShader[] pixelShaders;

            public Resources(IDevice device)
            {
                this.device = device;

                vertexShaders = new VertexShader[VertexShaderCount];
                pixelShaders = new PixelShader[PixelShaderCount];
                inputLayouts = new InputLayout[VertexShaderCount];
            }

            public VertexShader GetVertexShader(int index)
            {
                lock (this)
                {
                    if (vertexShaders[index] == null)
                    {
                        var definition = VertexShaderDefinitions[index];

                        vertexShaders[index] = device.CreateVertexShader();
                        vertexShaders[index].Name = definition.Name;
                        vertexShaders[index].Initialize(definition.Bytecode);
                    }

                    return vertexShaders[index];
                }
            }

            public PixelShader GetPixelShader(int index)
            {
                lock (this)
                {
                    if (pixelShaders[index] == null)
                    {
                        pixelShaders[index] = device.CreatePixelShader();
                        pixelShaders[index].Initialize(PixelShaderDefinitions[index].Bytecode);
                    }

                    return pixelShaders[index];
                }
            }
        }

        #endregion

        #region DirtyFlags

        [Flags]
        enum DirtyFlags
        {
            Contants                = 0x01,
            WorldViewProj           = 0x02,
            WorldInverseTranspose   = 0x04,
            EyePosition             = 0x08,
            MaterialColor           = 0x10,
            FogVector               = 0x20,
            FogEnable               = 0x40,
            //AlphaTest               = 0x80
        }

        #endregion

        #region Constants

        [StructLayout(LayoutKind.Sequential)]
        struct Constants
        {
            // http://msdn.microsoft.com/en-us/library/ff476182(v=vs.85).aspx
            // 定数バッファに限っては、D3D10 以降は 16 バイトの倍数サイズでなければならない。
            // StructLayout や FieldOffset を駆使しても、
            // 最終的には StructLayout で Size を明示しないと保証できない。
            // 全フィールドを 16 バイト基準の型 (Vector4 や Matrix) に揃えると、
            // 単純に 16 バイトの倍数サイズが確定する。
            // 見通しも良いので、これが最善と思われる。
            // (Direct Math で XMVECTOR や XMMATRIX に揃える事と同じ)。
            //
            // 配列を MarshalAs(ByValArray) で定義することはしない。
            // Marshal.SizeOf では期待のサイズを測れるが、
            // SharpDX の Utilities.SizeOf でもある sizeof ではポインタ サイズとなる。
            // これらの混在も怖い。
            // このため、基本的にはポインタを避けるため、配列を用いない。
            // 

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

        static readonly Dictionary<IDevice, Resources> ResourcesByDevice = new Dictionary<IDevice, Resources>();

        static BasicEffect()
        {
            //var compiler = new Compiler.ShaderCompiler();
            //compiler.RootPath = "Shaders";
            //var vsBasicVertexLighting = compiler.CompileFromFile("BasicEffect.fx", "VSBasicVertexLighting", Compiler.VertexShaderProfile.vs_4_0);
            //var psBasicVertexLightingNoFog = compiler.CompileFromFile("BasicEffect.fx", "PSBasicVertexLightingNoFog", Compiler.PixelShaderProfile.ps_4_0);

            VertexShaderDefinitions = new[]
            {
                new VertexShaderDefinition(VSBasic, "BasicEffect_VSBasic"),
                new VertexShaderDefinition(VSBasicNoFog, "BasicEffect_VSBasicNoFog"),
                new VertexShaderDefinition(VSBasicVc, "BasicEffect_VSBasicVc"),
                new VertexShaderDefinition(VSBasicVcNoFog, "BasicEffect_VSBasicVcNoFog"),
                new VertexShaderDefinition(VSBasicTx, "BasicEffect_VSBasicTx"),
                new VertexShaderDefinition(VSBasicTxNoFog, "BasicEffect_VSBasicTxNoFog"),
                new VertexShaderDefinition(VSBasicTxVc, "BasicEffect_VSBasicTxVc"),
                new VertexShaderDefinition(VSBasicTxVcNoFog, "BasicEffect_VSBasicTxVcNoFog"),

                new VertexShaderDefinition(VSBasicVertexLighting, "BasicEffect_VSBasicVertexLighting"),
                new VertexShaderDefinition(VSBasicVertexLightingVc, "BasicEffect_VSBasicVertexLightingVc"),
                new VertexShaderDefinition(VSBasicVertexLightingTx, "BasicEffect_VSBasicVertexLightingTx"),
                new VertexShaderDefinition(VSBasicVertexLightingTxVc, "BasicEffect_VSBasicVertexLightingTxVc"),

                new VertexShaderDefinition(VSBasicOneLight, "BasicEffect_VSBasicOneLight"),
                new VertexShaderDefinition(VSBasicOneLightVc, "BasicEffect_VSBasicOneLightVc"),
                new VertexShaderDefinition(VSBasicOneLightTx, "BasicEffect_VSBasicOneLightTx"),
                new VertexShaderDefinition(VSBasicOneLightTxVc, "BasicEffect_VSBasicOneLightTxVc"),

                new VertexShaderDefinition(VSBasicPixelLighting, "BasicEffect_VSBasicPixelLighting"),
                new VertexShaderDefinition(VSBasicPixelLightingVc, "BasicEffect_VSBasicPixelLightingVc"),
                new VertexShaderDefinition(VSBasicPixelLightingTx, "BasicEffect_VSBasicPixelLightingTx"),
                new VertexShaderDefinition(VSBasicPixelLightingTxVc, "BasicEffect_VSBasicPixelLightingTxVc"),
            };

            PixelShaderDefinitions = new[]
            {
                new PixelShaderDefinition(PSBasic, "BasicEffect_PSBasic"),
                new PixelShaderDefinition(PSBasicNoFog, "BasicEffect_PSBasicNoFog"),
                new PixelShaderDefinition(PSBasicTx, "BasicEffect_PSBasicTx"),
                new PixelShaderDefinition(PSBasicTxNoFog, "BasicEffect_PSBasicTxNoFog"),

                new PixelShaderDefinition(PSBasicVertexLighting, "BasicEffect_PSBasicVertexLighting"),
                new PixelShaderDefinition(PSBasicVertexLightingNoFog, "BasicEffect_PSBasicVertexLightingNoFog"),
                new PixelShaderDefinition(PSBasicVertexLightingTx, "BasicEffect_PSBasicVertexLightingTx"),
                new PixelShaderDefinition(PSBasicVertexLightingTxNoFog, "BasicEffect_PSBasicVertexLightingTxNoFog"),

                new PixelShaderDefinition(PSBasicPixelLighting, "BasicEffect_PSBasicPixelLighting"),
                new PixelShaderDefinition(PSBasicPixelLightingTx, "BasicEffect_PSBasicPixelLightingTx"),
            };
        }

        IDevice device;

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

        public BasicEffect(IDevice device)
        {
            if (device == null) throw new ArgumentNullException("device");

            this.device = device;

            directionalLights = new DirectionalLightCollection(this);

            constantBuffer = device.CreateConstantBuffer();
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

        public void Apply(DeviceContext context)
        {
            SetWorldViewProjConstant();
            SetFogConstants();
            SetLightConstants();

            if (TextureEnabled)
            {
                context.PixelShaderResources[0] = Texture;
            }

            ApplyShaders(context, GetCurrentShaderPermutation());
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

        void ApplyShaders(DeviceContext context, int permutation)
        {
            Resources resources;

            lock (ResourcesByDevice)
            {
                if (!ResourcesByDevice.TryGetValue(device, out resources))
                {
                    resources = new Resources(device);
                    ResourcesByDevice[device] = resources;
                }
            }

            var vertexShaderIndex = VertexShaderIndices[permutation];
            var pixelShaderIndex = PixelShaderIndices[permutation];

            var vertexShader = resources.GetVertexShader(vertexShaderIndex);
            var pixelShader = resources.GetPixelShader(pixelShaderIndex);

            context.VertexShader = vertexShader;
            context.PixelShader = pixelShader;

            if ((dirtyFlags & DirtyFlags.Contants) != 0)
            {
                constantBuffer.SetData(context, constants);
                dirtyFlags &= ~DirtyFlags.Contants;
            }

            context.VertexShaderConstantBuffers[0] = constantBuffer;
            context.PixelShaderConstantBuffers[0] = constantBuffer;
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
