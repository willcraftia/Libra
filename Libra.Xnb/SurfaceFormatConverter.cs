#region Using

using System;
using Libra.Graphics;

#endregion

namespace Libra.Xnb
{
    internal static class SurfaceFormatConverter
    {
        #region XnbSurfaceFormat

        enum XnbSurfaceFormat
        {
            Color           = 0,
            Bgr565          = 1,
            Bgra5551        = 2,
            Bgra4444        = 3,
            Dxt1            = 4,
            Dxt3            = 5,
            Dxt5            = 6,
            NormalizedByte2 = 7,
            NormalizedByte4 = 8,
            Rgba1010102     = 9,
            Rg32            = 10,
            Rgba64          = 11,
            Alpha8          = 12,
            Single          = 13,
            Vector2         = 14,
            Vector4         = 15,
            HalfSingle      = 16,
            HalfVector2     = 17,
            HalfVector4     = 18,
            HdrBlendable    = 19,
        }

        #endregion

        static readonly SurfaceFormat[] Mapping =
        {
            SurfaceFormat.Color,
            SurfaceFormat.Bgr565,
            SurfaceFormat.Bgra5551,
            SurfaceFormat.Bgra4444,
            SurfaceFormat.BC1,
            SurfaceFormat.BC2,
            SurfaceFormat.BC3,
            SurfaceFormat.NormalizedByte2,
            SurfaceFormat.NormalizedByte4,
            SurfaceFormat.Rgba1010102,
            SurfaceFormat.Rg32,
            SurfaceFormat.Rgba64,
            SurfaceFormat.Alpha8,
            SurfaceFormat.Single,
            SurfaceFormat.Vector2,
            SurfaceFormat.Vector4,
            SurfaceFormat.HalfSingle,
            SurfaceFormat.HalfVector2,
            SurfaceFormat.HalfVector4,
            0
        };

        public static SurfaceFormat ToSurfaceFormat(int xnbValue)
        {
            if ((uint) (Mapping.Length - 1) < (uint) xnbValue)
                throw new NotSupportedException("Unknown surface format: " + xnbValue);

            if (((XnbSurfaceFormat) xnbValue) == XnbSurfaceFormat.HdrBlendable)
                throw new NotSupportedException("HdrBlendable not supported.");

            return Mapping[xnbValue];
        }
    }
}
