#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public sealed class BlendState : State
    {
        public static readonly BlendState Additive;

        public static readonly BlendState AlphaBlend;

        public static readonly BlendState NonPremultiplied;

        public static readonly BlendState Opaque;

        Color blendFactor;

        Blend colorSourceBlend;

        Blend colorDestinationBlend;

        BlendFunction colorBlendFunction;

        Blend alphaSourceBlend;

        Blend alphaDestinationBlend;

        BlendFunction alphaBlendFunction;

        ColorWriteChannels colorWriteChannels;

        int multiSampleMask;

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

        //public ColorWriteChannels ColorWriteChannels1 { get; set; }

        //public ColorWriteChannels ColorWriteChannels2 { get; set; }

        //public ColorWriteChannels ColorWriteChannels3 { get; set; }

        public int MultiSampleMask
        {
            get { return multiSampleMask; }
            set
            {
                AssertNotFrozen();
                multiSampleMask = value;
            }
        }

        static BlendState()
        {
            Additive = new BlendState
            {
                ColorSourceBlend = Blend.SourceAlpha,
                AlphaSourceBlend = Blend.SourceAlpha,
                ColorDestinationBlend = Blend.One,
                AlphaDestinationBlend = Blend.One,
                Name = "Additive"
            };

            AlphaBlend = new BlendState
            {
                ColorSourceBlend = Blend.One,
                AlphaSourceBlend = Blend.One,
                ColorDestinationBlend = Blend.InverseSourceAlpha,
                AlphaDestinationBlend = Blend.InverseSourceAlpha,
                Name = "AlphaBlend"
            };

            NonPremultiplied = new BlendState
            {
                ColorSourceBlend = Blend.SourceAlpha,
                AlphaSourceBlend = Blend.SourceAlpha,
                ColorDestinationBlend = Blend.InverseSourceAlpha,
                AlphaDestinationBlend = Blend.InverseSourceAlpha,
                Name = "NonPremultiplied"
            };

            Opaque = new BlendState
            {
                ColorSourceBlend = Blend.One,
                AlphaSourceBlend = Blend.One,
                ColorDestinationBlend = Blend.Zero,
                AlphaDestinationBlend = Blend.Zero,
                Name = "Opaque"
            };
        }

        public BlendState()
        {
            colorSourceBlend = Blend.One;
            alphaSourceBlend = Blend.One;
            colorDestinationBlend = Blend.One;
            alphaDestinationBlend = Blend.One;

            colorBlendFunction = BlendFunction.Add;
            alphaBlendFunction = BlendFunction.Add;

            // XNA のドキュメントではデフォルト None らしいが、
            // DirectXTK では All 固定で設定している。
            colorWriteChannels = ColorWriteChannels.All;

            // TODO
            // これの意味がわからない。
            multiSampleMask = -1;

            blendFactor = Color.White;
        }
    }
}
