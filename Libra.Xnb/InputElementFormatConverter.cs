#region Using

using System;
using Libra.Graphics;

#endregion

namespace Libra.Xnb
{
    internal static class InputElementFormatConverter
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

        static readonly InputElementFormat[] Mapping =
        {
            InputElementFormat.Single,
            InputElementFormat.Vector2,
            InputElementFormat.Vector3,
            InputElementFormat.Vector4,
            InputElementFormat.Color,
            InputElementFormat.Byte4,
            InputElementFormat.Short2,
            InputElementFormat.Short4,
            InputElementFormat.NormalizedShort2,
            InputElementFormat.NormalizedShort4,
            InputElementFormat.HalfVector2,
            InputElementFormat.HalfVector4
        };

        public static InputElementFormat ToInputElement(int xnbValue)
        {
            if ((uint) (Mapping.Length - 1) < (uint) xnbValue)
                throw new NotSupportedException("Unknown vertex element format: " + xnbValue);

            return Mapping[xnbValue];
        }
    }
}
