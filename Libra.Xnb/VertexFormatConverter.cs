#region Using

using System;
using Libra.Graphics;

#endregion

namespace Libra.Xnb
{
    internal static class VertexFormatConverter
    {
        #region XnbVertexElementFormat

        enum XnbVertexElementFormat
        {
            Single              = 0,
            Vector2             = 1,
            Vector3             = 2,
            Vector4             = 3,
            Color               = 4,
            Byte4               = 5,
            Short2              = 6,
            Short4              = 7,
            NormalizedShort2    = 8,
            NormalizedShort4    = 9,
            HalfVector2         = 10,
            HalfVector4         = 11,
        }

        #endregion

        static readonly VertexFormat[] Mapping =
        {
            VertexFormat.Single,
            VertexFormat.Vector2,
            VertexFormat.Vector3,
            VertexFormat.Vector4,
            VertexFormat.Color,
            VertexFormat.Byte4,
            VertexFormat.Short2,
            VertexFormat.Short4,
            VertexFormat.NormalizedShort2,
            VertexFormat.NormalizedShort4,
            VertexFormat.HalfVector2,
            VertexFormat.HalfVector4
        };

        public static VertexFormat ToInputElement(int xnbValue)
        {
            if ((uint) (Mapping.Length - 1) < (uint) xnbValue)
                throw new NotSupportedException("Unknown vertex element format: " + xnbValue);

            return Mapping[xnbValue];
        }
    }
}
