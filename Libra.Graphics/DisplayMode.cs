#region Using

using System;

#endregion

namespace Libra.Graphics
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// DXGI_MODE_DESC。
    /// </remarks>
    public struct DisplayMode : IEquatable<DisplayMode>
    {
        public int Width;

        public int Height;

        public RefreshRate RefreshRate;

        public SurfaceFormat Format;

        public DisplayModeScanlineOrder ScanlineOrdering;

        public DisplayModeScaling Scaling;

        public DisplayMode(int width, int height, RefreshRate refreshRate, SurfaceFormat format)
        {
            Width = width;
            Height = height;
            RefreshRate = refreshRate;
            Format = format;
            ScanlineOrdering = DisplayModeScanlineOrder.Unspecified;
            Scaling = DisplayModeScaling.Unspecified;
        }

        #region Equatable

        public static bool operator ==(DisplayMode value1, DisplayMode value2)
        {
            return value1.Equals(value2);
        }

        public static bool operator !=(DisplayMode value1, DisplayMode value2)
        {
            return !value1.Equals(value2);
        }

        public bool Equals(DisplayMode other)
        {
            return Width == other.Width && Height == other.Height &&
                RefreshRate == other.RefreshRate && Format == other.Format;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((DisplayMode) obj);
        }

        public override int GetHashCode()
        {
            return Width.GetHashCode() ^ Height.GetHashCode() ^
                RefreshRate.GetHashCode() ^ Format.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{Width:" + Width + " Height:" + Height +
                " RefreshRate:" + RefreshRate + " Format:" + Format +
                " ScanlineOrdering:" + ScanlineOrdering +
                " Scaling:" + Scaling + "}";
        }

        #endregion
    }
}
