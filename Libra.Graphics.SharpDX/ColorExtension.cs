#region Using

using System;

using SDXColor4 = SharpDX.Color4;

#endregion

namespace Libra.Graphics.SharpDX
{
    public static class ColorExtension
    {
        public static SDXColor4 ToSDXColor4(this Color color)
        {
            var vector4 = color.ToVector4();
            return new SDXColor4(vector4.X, vector4.Y, vector4.Z, vector4.W);
        }
    }
}
