#region Using

using System;
using Libra.Graphics.Properties;

#endregion

namespace Libra.Graphics
{
    public sealed class AlphaTestEffect : IEffect, IEffectMatrices, IEffectFog
    {
        #region ShaderDefinition

        sealed class ShaderDefinition
        {
            public byte[] Bytecode;

            public string Name;

            public ShaderDefinition(byte[] bytecode, string name)
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
                        var definition = PixelShaderDefinitions[index];

                        pixelShaders[index] = device.CreatePixelShader();
                        pixelShaders[index].Name = definition.Name;
                        pixelShaders[index].Initialize(definition.Bytecode);
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
            Contants        = (1 << 0),
            WorldViewProj   = (1 << 1),
            MaterialColor   = (1 << 2),
            FogVector       = (1 << 3),
            FogEnable       = (1 << 4),
            AlphaTest       = (1 << 5)
        }

        #endregion

        #region Constants

        struct Constants
        {
            public Vector4 DiffuseColor;

            public Vector4 AlphaTest;

            public Vector4 FogColor;

            public Vector4 FogVector;

            public Matrix WorldViewProj;
        }

        #endregion

        const int VertexShaderCount = 4;

        const int PixelShaderCount = 4;

        const int ShaderPermutationCount = 8;

        static readonly ShaderDefinition[] VertexShaderDefinitions =
        {
            new ShaderDefinition(Resources.AlphaTestEffectVSAlphaTest,          "AlphaTestEffectVSAlphaTest"),
            new ShaderDefinition(Resources.AlphaTestEffectVSAlphaTestNoFog,     "AlphaTestEffectVSAlphaTestNoFog"),
            new ShaderDefinition(Resources.AlphaTestEffectVSAlphaTestVc,        "AlphaTestEffectVSAlphaTestVc"),
            new ShaderDefinition(Resources.AlphaTestEffectVSAlphaTestVcNoFog,   "AlphaTestEffectVSAlphaTestVcNoFog"),
        };

        static readonly ShaderDefinition[] PixelShaderDefinitions =
        {
            new ShaderDefinition(Resources.AlphaTestEffectPSAlphaTestLtGt,      "AlphaTestEffectPSAlphaTestLtGt"),
            new ShaderDefinition(Resources.AlphaTestEffectPSAlphaTestLtGtNoFog, "AlphaTestEffectPSAlphaTestLtGtNoFog"),
            new ShaderDefinition(Resources.AlphaTestEffectPSAlphaTestEqNe,      "AlphaTestEffectPSAlphaTestEqNe"),
            new ShaderDefinition(Resources.AlphaTestEffectPSAlphaTestEqNeNoFog, "AlphaTestEffectPSAlphaTestEqNeNoFog"),
        };

        static readonly int[] VertexShaderIndices =
        {
            0,      // lt/gt
            1,      // lt/gt, no fog
            2,      // lt/gt, vertex color
            3,      // lt/gt, vertex color, no fog
    
            0,      // eq/ne
            1,      // eq/ne, no fog
            2,      // eq/ne, vertex color
            3,      // eq/ne, vertex color, no fog
        };

        static readonly int[] PixelShaderIndices =
        {
            0,      // lt/gt
            1,      // lt/gt, no fog
            0,      // lt/gt, vertex color
            1,      // lt/gt, vertex color, no fog
    
            2,      // eq/ne
            3,      // eq/ne, no fog
            2,      // eq/ne, vertex color
            3,      // eq/ne, vertex color, no fog
        };

        static readonly Vector2 SelectIfTrue  = new Vector2( 1, -1);

        static readonly Vector2 SelectIfFalse = new Vector2(-1,  1);
        
        static readonly Vector2 SelectNever   = new Vector2(-1, -1);
        
        static readonly Vector2 SelectAlways  = new Vector2( 1,  1);

        SharedDeviceResource sharedDeviceResource;

        DirtyFlags dirtyFlags;

        Constants constants;

        Matrix world;

        Matrix view;

        Matrix projection;

        Vector3 diffuseColor;

        float alpha;

        bool fogEnabled;

        float fogStart;

        float fogEnd;

        ComparisonFunction alphaFunction;

        int referenceAlpha;

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
                dirtyFlags |= DirtyFlags.WorldViewProj | DirtyFlags.FogVector;
            }
        }

        public Matrix View
        {
            get { return view; }
            set
            {
                view = value;
                dirtyFlags |= DirtyFlags.WorldViewProj | DirtyFlags.FogVector;
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

        public float Alpha
        {
            get { return alpha; }
            set
            {
                alpha = value;
                dirtyFlags |= DirtyFlags.MaterialColor;
            }
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

        public ComparisonFunction AlphaFunction
        {
            get { return alphaFunction; }
            set
            {
                if (alphaFunction == value) return;

                alphaFunction = value;

                dirtyFlags |= DirtyFlags.AlphaTest;
            }
        }

        public int ReferenceAlpha
        {
            get { return referenceAlpha; }
            set
            {
                if (referenceAlpha == value) return;

                referenceAlpha = value;

                dirtyFlags |= DirtyFlags.AlphaTest;
            }
        }

        public bool VertexColorEnabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public AlphaTestEffect(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;

            sharedDeviceResource = deviceContext.Device.GetSharedResource<AlphaTestEffect, SharedDeviceResource>();

            constantBuffer = deviceContext.Device.CreateConstantBuffer();
            constantBuffer.Usage = ResourceUsage.Dynamic;
            constantBuffer.Initialize<Constants>();

            // デフォルト値。
            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;

            diffuseColor = Vector3.One;
            alpha = 1;

            fogEnabled = false;
            fogStart = 0;
            fogEnd = 1;

            alphaFunction = ComparisonFunction.Greater;
            referenceAlpha = 0;

            dirtyFlags =
                DirtyFlags.Contants |
                DirtyFlags.WorldViewProj |
                DirtyFlags.MaterialColor |
                DirtyFlags.FogVector |
                DirtyFlags.FogEnable;
        }

        public void Apply()
        {
            SetWorldViewProjConstant();
            SetFogConstants();
            SetMaterialColorConstants();
            SetAlphaTestConstants();

            DeviceContext.PixelShaderResources[0] = Texture;

            ApplyShaders(GetCurrentShaderPermutation());
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

        void SetMaterialColorConstants()
        {
            if ((dirtyFlags & DirtyFlags.MaterialColor) != 0)
            {
                constants.DiffuseColor.X = diffuseColor.X * alpha;
                constants.DiffuseColor.Y = diffuseColor.Y * alpha;
                constants.DiffuseColor.Z = diffuseColor.Z * alpha;
                constants.DiffuseColor.W = alpha;

                dirtyFlags &= ~DirtyFlags.MaterialColor;
                dirtyFlags |= DirtyFlags.Contants;
            }
        }

        void SetAlphaTestConstants()
        {
            if ((dirtyFlags & DirtyFlags.AlphaTest) != 0)
            {
                float reference = (float) referenceAlpha / 255.0f;

                const float threshold = 0.5f / 255.0f;

                float compareTo;
                Vector2 selector;

                switch (alphaFunction)
                {
                    case ComparisonFunction.Less:
                        compareTo = reference - threshold;
                        selector = SelectIfTrue;
                        break;
                    case ComparisonFunction.LessEqual:
                        compareTo = reference + threshold;
                        selector = SelectIfTrue;
                        break;
                    case ComparisonFunction.GreaterEqual:
                        compareTo = reference - threshold;
                        selector = SelectIfFalse;
                        break;
                    case ComparisonFunction.Greater:
                        compareTo = reference + threshold;
                        selector = SelectIfFalse;
                        break;
                    case ComparisonFunction.Equal:
                        compareTo = reference;
                        selector = SelectIfTrue;
                        break;
                    case ComparisonFunction.NotEqual:
                        compareTo = reference;
                        selector = SelectIfFalse;
                        break;
                    case ComparisonFunction.Never:
                        compareTo = 0;
                        selector = SelectNever;
                        break;
                    case ComparisonFunction.Always:
                        compareTo = 0;
                        selector = SelectAlways;
                        break;
                    default:
                        throw new InvalidOperationException("Invalid alpha test function: " + alphaFunction);
                }

                constants.AlphaTest.X = compareTo;
                constants.AlphaTest.Y = threshold;
                constants.AlphaTest.Z = selector.X;
                constants.AlphaTest.W = selector.Y;
            }

            dirtyFlags &= ~DirtyFlags.AlphaTest;
            dirtyFlags |= DirtyFlags.Contants;
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

            if (alphaFunction == ComparisonFunction.Equal ||
                alphaFunction == ComparisonFunction.NotEqual)
            {
                permutation += 4;
            }

            return permutation;
        }

        void ApplyShaders(int permutation)
        {
            DeviceContext.VertexShader = sharedDeviceResource.GetVertexShader(permutation);
            DeviceContext.PixelShader = sharedDeviceResource.GetPixelShader(permutation);

            if ((dirtyFlags & DirtyFlags.Contants) != 0)
            {
                DeviceContext.SetData(constantBuffer, constants);

                dirtyFlags &= ~DirtyFlags.Contants;
            }

            DeviceContext.VertexShaderConstantBuffers[0] = constantBuffer;
            DeviceContext.PixelShaderConstantBuffers[0] = constantBuffer;
        }

        #region IDisposable

        bool disposed;

        ~AlphaTestEffect()
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
                //constantBuffer.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
