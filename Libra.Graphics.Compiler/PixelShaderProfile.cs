#region Using

using System;

#endregion

namespace Libra.Graphics.Compiler
{
    public enum PixelShaderProfile
    {
        // D3D 9 SM 2.0
        ps_2_0,
        ps_2_a,
        ps_2_b,
        ps_2_sw,

        // D3D 9 SM 3.0
        ps_3_0,
        ps_3_sw,

        // D3D 9.1/9.2/9.3
        ps_4_0_level_9_1,
        ps_4_0_level_9_3,

        // D3D 10.0
        ps_4_0,

        // D3D 10.1
        ps_4_1,

        // D3D 11.0/11.1
        ps_5_0
    }
}
