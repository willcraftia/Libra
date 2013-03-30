#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public sealed class SamplerState : State
    {
        public static readonly SamplerState AnisotropicClamp;

        public static readonly SamplerState AnisotropicWrap;

        public static readonly SamplerState LinearClamp;

        public static readonly SamplerState LinearWrap;

        public static readonly SamplerState PointClamp;

        public static readonly SamplerState PointWrap;

        TextureFilter filter;

        TextureAddressMode addressU;

        TextureAddressMode addressV;

        TextureAddressMode addressW;

        float minLod;

        float maxLod;

        float mipMapLodBias;

        int maxAnisotropy;

        ComparisonFunction comparisonFunction;

        Color borderColor;

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

        static SamplerState()
        {
            AnisotropicClamp = new SamplerState
            {
                Filter = TextureFilter.Anisotropic,
                Name = "AnisotropicClamp"
            };

            AnisotropicWrap = new SamplerState
            {
                Filter = TextureFilter.Anisotropic,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Name = "AnisotropicWrap"
            };

            LinearClamp = new SamplerState
            {
                Name = "LinearClamp"
            };

            LinearWrap = new SamplerState
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Name = "LinearWrap"
            };

            PointClamp = new SamplerState
            {
                Filter = TextureFilter.MinMagMipPoint,
                Name = "PointClamp"
            };

            PointWrap = new SamplerState
            {
                Filter = TextureFilter.MinMagMipPoint,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Name = "PointWrap"
            };
        }

        public SamplerState()
        {
            Filter = TextureFilter.MinMagMipLinear;
            AddressU = TextureAddressMode.Clamp;
            AddressV = TextureAddressMode.Clamp;
            AddressW = TextureAddressMode.Clamp;
            MinLod = float.MinValue;
            MaxLod = float.MaxValue;
            MipMapLodBias = 0;
            MaxAnisotropy = 16;
            ComparisonFunction = ComparisonFunction.Never;
            BorderColor = Color.Black;
        }
    }
}
