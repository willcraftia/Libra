#region Using

using System;

#endregion

namespace Libra.Graphics
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// DXGI_MODE_SCANLINE_ORDER。
    /// </remarks>
    public enum DisplayModeScanlineOrder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED。
        /// </remarks>
        Unspecified     = 0,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// DXGI_MODE_SCANLINE_ORDER_PROGRESSIVE。
        /// </remarks>
        Progressive     = 1,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// DXGI_MODE_SCANLINE_ORDER_UPPER_FIELD_FIRST。
        /// </remarks>
        UpperFieldFirst = 2,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// DXGI_MODE_SCANLINE_ORDER_LOWER_FIELD_FIRST。
        /// </remarks>
        LowerFieldFirst = 3,
    }
}
