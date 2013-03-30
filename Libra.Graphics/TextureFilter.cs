#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public enum TextureFilter
    {
        MinMagMipPoint                          = 0,
        MinMagPointMipLinear                    = 0x1,
        MinPointMagLinearMipPoint               = 0x4,
        MinPointMagMipLinear                    = 0x5,
        MinLinearMagMipPoint                    = 0x10,
        MinLinearMagPointMipLinear              = 0x11,
        MinMagLinearMipPoint                    = 0x14,
        MinMagMipLinear                         = 0x15,
        Anisotropic                             = 0x55,
        ComparisonMinMagMipPoint                = 0x80,
        ComparisonMinMagPointMipLinear          = 0x81,
        ComparisonMinPointMagLinearMipPoint     = 0x84,
        ComparisonMinPointMagMipLinear          = 0x85,
        ComparisonMinLinearMagMipPoint          = 0x90,
        ComparisonMinLinearMagPointMipLinear    = 0x91,
        ComparisonMinMagLinearMipPoint          = 0x94,
        ComparisonMinMagMipLinear               = 0x95,
        ComparisonAnisotropic                   = 0xd5,
    }
}
