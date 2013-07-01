#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public sealed class RasterizerState : State
    {
        public static readonly RasterizerState CullNone = new RasterizerState
        {
            CullMode = CullMode.None,
            Name = "CullNone"
        };

        public static readonly RasterizerState CullFront = new RasterizerState
        {
            CullMode = CullMode.Front,
            Name = "CullFront"
        };

        public static readonly RasterizerState CullBack = new RasterizerState
        {
            Name = "CullBack"
        };

        public static readonly RasterizerState Wireframe = new RasterizerState
        {
            FillMode = FillMode.Wireframe,
            CullMode = CullMode.None,
            Name = "Wireframe"
        };

        FillMode fillMode = FillMode.Solid;

        CullMode cullMode = CullMode.Back;

        int depthBias;

        float slopeScaleDepthBias;

        float depthBiasClamp;

        bool depthClipEnable = true;

        bool scissorEnable;

        bool multisampleEnable;

        bool antialiasedLineEnable;

        public FillMode FillMode
        {
            get { return fillMode; }
            set
            {
                AssertNotFrozen();
                fillMode = value;
            }
        }

        public CullMode CullMode
        {
            get { return cullMode; }
            set
            {
                AssertNotFrozen();
                cullMode = value;
            }
        }

        public int DepthBias
        {
            get { return depthBias; }
            set
            {
                AssertNotFrozen();
                depthBias = value;
            }
        }

        public float SlopeScaleDepthBias
        {
            get { return slopeScaleDepthBias; }
            set
            {
                AssertNotFrozen();
                slopeScaleDepthBias = value;
            }
        }

        public float DepthBiasClamp
        {
            get { return depthBiasClamp; }
            set
            {
                AssertNotFrozen();
                depthBiasClamp = value;
            }
        }

        public bool DepthClipEnable
        {
            get { return depthClipEnable; }
            set
            {
                AssertNotFrozen();
                depthClipEnable = value;
            }
        }

        public bool ScissorEnable
        {
            get { return scissorEnable; }
            set
            {
                AssertNotFrozen();
                scissorEnable = value;
            }
        }

        public bool MultisampleEnable
        {
            get { return multisampleEnable; }
            set
            {
                AssertNotFrozen();
                multisampleEnable = value;
            }
        }

        public bool AntialiasedLineEnable
        {
            get { return antialiasedLineEnable; }
            set
            {
                AssertNotFrozen();
                antialiasedLineEnable = value;
            }
        }
    }
}
