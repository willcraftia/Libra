#region Using

using System;
using System.Collections.Generic;

using DXGIDisplayModeEnumerationFlags = SharpDX.DXGI.DisplayModeEnumerationFlags;
using DXGIDisplayModeScaling = SharpDX.DXGI.DisplayModeScaling;
using DXGIDisplayModeScanlineOrder = SharpDX.DXGI.DisplayModeScanlineOrder;
using DXGIFormat = SharpDX.DXGI.Format;
using DXGIModeDescription = SharpDX.DXGI.ModeDescription;
using DXGIOutput = SharpDX.DXGI.Output;

#endregion

namespace Libra.Graphics.SharpDX
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class SdxOutput : IOutput
    {
        public string DeviceName { get; private set; }

        public Rectangle DesktopCoordinates { get; private set; }

        public bool AttachedToDesktop { get; private set; }

        public IntPtr Monitor { get; private set; }

        DXGIOutput dxgiOutput;

        internal SdxOutput(DXGIOutput dxgiOutput)
        {
            this.dxgiOutput = dxgiOutput;

            var outpuDescription = dxgiOutput.Description;

            DeviceName = outpuDescription.DeviceName;
            DesktopCoordinates = new Rectangle
            {
                X = outpuDescription.DesktopBounds.Left,
                Y = outpuDescription.DesktopBounds.Top,
                Width = outpuDescription.DesktopBounds.Width,
                Height = outpuDescription.DesktopBounds.Height
            };
            AttachedToDesktop = outpuDescription.IsAttachedToDesktop;
            Monitor = outpuDescription.MonitorHandle;
        }

        // メモ
        //
        // 表示モードの列挙が必要な場合にのみ、それらを列挙するものとし、
        // XNA の GraphicsAdapter のような事前列挙による保持はしない。
        // 殆どの場合において、GetClosestMatchingMode による最適表示モードの検索で十分であり、
        // 事前列挙の必要性が少ない。

        public DisplayMode[] GetModes(SurfaceFormat format, EnumerateDisplayModes flags)
        {
            var dxgiModes = dxgiOutput.GetDisplayModeList((DXGIFormat) format, (DXGIDisplayModeEnumerationFlags) flags);

            var result = new DisplayMode[dxgiModes.Length];
            for (int i = 0; i < dxgiModes.Length; i++)
                FromDXGIModeDescription(ref dxgiModes[i], out result[i]);

            return result;
        }

        public void GetClosestMatchingMode(IDevice device, ref DisplayMode preferredMode, out DisplayMode result)
        {
            var d3d11Device = (device as SdxDevice).D3D11Device;

            DXGIModeDescription dxgiModeToMatch;
            ToDXGIModeDescription(ref preferredMode, out dxgiModeToMatch);

            DXGIModeDescription dxgiResult;
            dxgiOutput.GetClosestMatchingMode(d3d11Device, dxgiModeToMatch, out dxgiResult);

            FromDXGIModeDescription(ref dxgiResult, out result);
        }

        internal void FromDXGIModeDescription(ref DXGIModeDescription dxgiMode, out DisplayMode result)
        {
            result = new DisplayMode
            {
                Width = dxgiMode.Width,
                Height = dxgiMode.Height,
                RefreshRate =
                {
                    Numerator = dxgiMode.RefreshRate.Numerator,
                    Denominator = dxgiMode.RefreshRate.Denominator
                },
                Format = (SurfaceFormat) dxgiMode.Format,
                ScanlineOrdering = (DisplayModeScanlineOrder) dxgiMode.ScanlineOrdering,
                Scaling = (DisplayModeScaling) dxgiMode.Scaling
            };
        }

        internal void ToDXGIModeDescription(ref DisplayMode mode, out DXGIModeDescription result)
        {
            result = new DXGIModeDescription
            {
                Width = mode.Width,
                Height = mode.Height,
                RefreshRate =
                {
                    Numerator = mode.RefreshRate.Numerator,
                    Denominator = mode.RefreshRate.Denominator
                },
                Format = (DXGIFormat) mode.Format,
                ScanlineOrdering = (DXGIDisplayModeScanlineOrder) mode.ScanlineOrdering,
                Scaling = (DXGIDisplayModeScaling) mode.Scaling
            };
        }
    }
}
