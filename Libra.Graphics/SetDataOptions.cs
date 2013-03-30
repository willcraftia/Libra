#region Using

using System;

#endregion

namespace Libra.Graphics
{
    // Shawn Hargreaves Blog - SetDataOptions: NoOverwrite versus Discard
    // http://blogs.msdn.com/b/shawnhar/archive/2010/07/07/setdataoptions-nooverwrite-versus-discard.aspx

    public enum SetDataOptions
    {
        // D3D11_MAP の値と等値。
        // XNA の SetDataOptions はフラグだが、
        // D3D11_MAP はフラグではないため注意。

        // D3D11_MAP_WRITE
        None        = 2,

        // D3D11_MAP_WRITE_DISCARD
        Discard     = 4,

        // D3D11_MAP_WRITE_NO_OVERWRITE
        NoOverwrite = 5,
    }
}
