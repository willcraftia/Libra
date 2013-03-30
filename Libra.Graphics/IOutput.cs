#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public interface IOutput
    {
        string DeviceName { get; }

        Rectangle DesktopCoordinates { get; }

        bool AttachedToDesktop { get; }

        IntPtr Monitor { get; }

        DisplayMode[] GetModes(SurfaceFormat format, EnumerateDisplayModes flags);

        void GetClosestMatchingMode(IDevice device, ref DisplayMode preferredMode, out DisplayMode result);
    }
}
