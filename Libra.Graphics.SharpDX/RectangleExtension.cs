#region Using

using System;

using SDXRectangle = SharpDX.Rectangle;

#endregion

namespace Libra.Graphics.SharpDX
{
    public static class RectangleExtension
    {
        public static SDXRectangle ToSDXRectangle(this Rectangle rectangle)
        {
            return new SDXRectangle(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        }
    }
}
