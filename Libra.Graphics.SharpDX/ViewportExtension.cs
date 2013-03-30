#region Using

using System;

using SDXViewportF = SharpDX.ViewportF;

#endregion

namespace Libra.Graphics.SharpDX
{
    public static class ViewportExtension
    {
        internal static SDXViewportF ToSDXViewportF(this Viewport viewport)
        {
            return new SDXViewportF(
                viewport.X, viewport.Y,
                viewport.Width, viewport.Height,
                viewport.MinDepth, viewport.MaxDepth);
        }
    }
}
