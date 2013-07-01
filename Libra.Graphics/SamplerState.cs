#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public sealed class SamplerState : State
    {
        public static readonly SamplerState AnisotropicClamp = new SamplerState
        {
            Filter = TextureFilter.Anisotropic,
            Name = "AnisotropicClamp"
        };

        public static readonly SamplerState AnisotropicWrap = new SamplerState
        {
            Filter = TextureFilter.Anisotropic,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            Name = "AnisotropicWrap"
        };

        public static readonly SamplerState AnisotropicMirror = new SamplerState
        {
            Filter = TextureFilter.Anisotropic,
            AddressU = TextureAddressMode.Mirror,
            AddressV = TextureAddressMode.Mirror,
            AddressW = TextureAddressMode.Mirror,
            Name = "AnisotropicMirror"
        };

        public static readonly SamplerState LinearClamp = new SamplerState
        {
            Name = "LinearClamp"
        };

        public static readonly SamplerState LinearWrap = new SamplerState
        {
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            Name = "LinearWrap"
        };

        public static readonly SamplerState LinearMirror = new SamplerState
        {
            AddressU = TextureAddressMode.Mirror,
            AddressV = TextureAddressMode.Mirror,
            AddressW = TextureAddressMode.Mirror,
            Name = "LinearMirror"
        };

        public static readonly SamplerState PointClamp = new SamplerState
        {
            Filter = TextureFilter.MinMagMipPoint,
            Name = "PointClamp"
        };

        public static readonly SamplerState PointWrap = new SamplerState
        {
            Filter = TextureFilter.MinMagMipPoint,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            Name = "PointWrap"
        };

        public static readonly SamplerState PointMirror = new SamplerState
        {
            Filter = TextureFilter.MinMagMipPoint,
            AddressU = TextureAddressMode.Mirror,
            AddressV = TextureAddressMode.Mirror,
            AddressW = TextureAddressMode.Mirror,
            Name = "PointMirror"
        };

        TextureFilter filter = TextureFilter.MinMagMipLinear;

        TextureAddressMode addressU = TextureAddressMode.Clamp;

        TextureAddressMode addressV = TextureAddressMode.Clamp;

        TextureAddressMode addressW = TextureAddressMode.Clamp;

        float minLod = float.MinValue;

        float maxLod = float.MaxValue;

        float mipMapLodBias;

        int maxAnisotropy = 16;

        ComparisonFunction comparisonFunction = ComparisonFunction.Never;

        Color borderColor = Color.Black;

        public TextureFilter Filter
        {
            get { return filter; }
            set
            {
                AssertNotFrozen();
                filter = value;
            }
        }

        public TextureAddressMode AddressU
        {
            get { return addressU; }
            set
            {
                AssertNotFrozen();
                addressU = value;
            }
        }

        public TextureAddressMode AddressV
        {
            get { return addressV; }
            set
            {
                AssertNotFrozen();
                addressV = value;
            }
        }

        public TextureAddressMode AddressW
        {
            get { return addressW; }
            set
            {
                AssertNotFrozen();
                addressW = value;
            }
        }

        public float MinLod
        {
            get { return minLod; }
            set
            {
                AssertNotFrozen();
                minLod = value;
            }
        }

        public float MaxLod
        {
            get { return maxLod; }
            set
            {
                AssertNotFrozen();
                maxLod = value;
            }
        }

        public float MipMapLodBias
        {
            get { return mipMapLodBias; }
            set
            {
                AssertNotFrozen();
                mipMapLodBias = value;
            }
        }

        public int MaxAnisotropy
        {
            get { return maxAnisotropy; }
            set
            {
                AssertNotFrozen();
                maxAnisotropy = value;
            }
        }

        public ComparisonFunction ComparisonFunction
        {
            get { return comparisonFunction; }
            set
            {
                AssertNotFrozen();
                comparisonFunction = value;
            }
        }

        public Color BorderColor
        {
            get { return borderColor; }
            set
            {
                AssertNotFrozen();
                borderColor = value;
            }
        }
    }
}
