#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public sealed class BlendState : State
    {
        public static readonly BlendState Additive = new BlendState
        {
            ColorSourceBlend = Blend.SourceAlpha,
            AlphaSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
            Name = "Additive"
        };

        public static readonly BlendState AlphaBlend = new BlendState
        {
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
            Name = "AlphaBlend"
        };

        public static readonly BlendState NonPremultiplied = new BlendState
        {
            ColorSourceBlend = Blend.SourceAlpha,
            AlphaSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
            Name = "NonPremultiplied"
        };

        public static readonly BlendState Opaque = new BlendState
        {
            Name = "Opaque"
        };

        public static readonly BlendState ColorWriteDisable = new BlendState
        {
            ColorWriteChannels = ColorWriteChannels.None,
            Name = "ColorWriteDisable"
        };

        Color blendFactor = Color.White;

        Blend colorSourceBlend = Blend.One;

        Blend colorDestinationBlend = Blend.Zero;

        BlendFunction colorBlendFunction = BlendFunction.Add;

        Blend alphaSourceBlend = Blend.One;

        Blend alphaDestinationBlend = Blend.Zero;

        BlendFunction alphaBlendFunction = BlendFunction.Add;

        // XNA のドキュメントではデフォルト None らしいが、
        // DirectXTK では All 固定で設定している。
        ColorWriteChannels colorWriteChannels = ColorWriteChannels.All;

        int multiSampleMask = -1;

        public Color BlendFactor
        {
            get { return blendFactor; }
            set
            {
                AssertNotFrozen();
                blendFactor = value;
            }
        }

        public Blend ColorSourceBlend
        {
            get { return colorSourceBlend; }
            set
            {
                AssertNotFrozen();
                colorSourceBlend = value;
            }
        }

        public Blend ColorDestinationBlend
        {
            get { return colorDestinationBlend; }
            set
            {
                AssertNotFrozen();
                colorDestinationBlend = value;
            }
        }

        public BlendFunction ColorBlendFunction
        {
            get { return colorBlendFunction; }
            set
            {
                AssertNotFrozen();
                colorBlendFunction = value;
            }
        }

        public Blend AlphaSourceBlend
        {
            get { return alphaSourceBlend; }
            set
            {
                AssertNotFrozen();
                alphaSourceBlend = value;
            }
        }

        public Blend AlphaDestinationBlend
        {
            get { return alphaDestinationBlend; }
            set
            {
                AssertNotFrozen();
                alphaDestinationBlend = value;
            }
        }

        public BlendFunction AlphaBlendFunction
        {
            get { return alphaBlendFunction; }
            set
            {
                AssertNotFrozen();
                alphaBlendFunction = value;
            }
        }

        public ColorWriteChannels ColorWriteChannels
        {
            get { return colorWriteChannels; }
            set
            {
                AssertNotFrozen();
                colorWriteChannels = value;
            }
        }

        public int MultiSampleMask
        {
            get { return multiSampleMask; }
            set
            {
                AssertNotFrozen();
                multiSampleMask = value;
            }
        }
    }
}
