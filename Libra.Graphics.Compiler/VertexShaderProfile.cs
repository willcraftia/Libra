#region Using

using System;

#endregion

namespace Libra.Graphics.Compiler
{
    public enum VertexShaderProfile
    {
        // D3D9 SM 1.x
        vs_1_1,

        // D3D 9 SM 2.0
        vs_2_0,
        vs_2_a,
        vs_2_sw,

        // D3D 9 SM 3.0
        vs_3_0,
        vs_3_0_sw,

        // D3D 9.1/9.2/9.3
        vs_4_0_level_9_1,
        vs_4_0_level_9_3,

        // D3D 10.0
        vs_4_0,

        // D3D 10.1
        vs_4_1,

        // D3D 11.0/11.1
        vs_5_0
    }
}
