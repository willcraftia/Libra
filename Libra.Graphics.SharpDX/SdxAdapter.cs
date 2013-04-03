#region Using

using System;

using DXGIAdapter1 = SharpDX.DXGI.Adapter1;
using DXGIDisplayModeEnumerationFlags = SharpDX.DXGI.DisplayModeEnumerationFlags;
using DXGIDisplayModeScaling = SharpDX.DXGI.DisplayModeScaling;
using DXGIDisplayModeScanlineOrder = SharpDX.DXGI.DisplayModeScanlineOrder;
using DXGIFormat = SharpDX.DXGI.Format;
using DXGIModeDescription = SharpDX.DXGI.ModeDescription;
using DXGIOutput = SharpDX.DXGI.Output;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxAdapter : Adapter
    {
        #region SdxOutput

        public sealed class SdxOutput : Output
        {
            string deviceName;

            Rectangle desktopCoordinates;

            bool attachedToDesktop;

            IntPtr monitor;

            public override string DeviceName
            {
                get { return deviceName; }
            }

            public override Rectangle DesktopCoordinates
            {
                get { return desktopCoordinates; }
            }

            public override bool AttachedToDesktop
            {
                get { return attachedToDesktop; }
            }

            public override IntPtr Monitor
            {
                get { return monitor; }
            }

            DXGIOutput dxgiOutput;

            internal SdxOutput(DXGIOutput dxgiOutput)
            {
                this.dxgiOutput = dxgiOutput;

                var outpuDescription = dxgiOutput.Description;

                deviceName = outpuDescription.DeviceName;
                desktopCoordinates = new Rectangle
                {
                    X = outpuDescription.DesktopBounds.Left,
                    Y = outpuDescription.DesktopBounds.Top,
                    Width = outpuDescription.DesktopBounds.Width,
                    Height = outpuDescription.DesktopBounds.Height
                };
                attachedToDesktop = outpuDescription.IsAttachedToDesktop;
                monitor = outpuDescription.MonitorHandle;
            }

            // メモ
            //
            // 表示モードの列挙が必要な場合にのみ、それらを列挙するものとし、
            // XNA の GraphicsAdapter のような事前列挙による保持はしない。
            // 殆どの場合において、GetClosestMatchingMode による最適表示モードの検索で十分であり、
            // 事前列挙の必要性が少ない。

            public override DisplayMode[] GetModes(SurfaceFormat format, EnumerateDisplayModes flags)
            {
                var dxgiModes = dxgiOutput.GetDisplayModeList((DXGIFormat) format, (DXGIDisplayModeEnumerationFlags) flags);

                var result = new DisplayMode[dxgiModes.Length];
                for (int i = 0; i < dxgiModes.Length; i++)
                    FromDXGIModeDescription(ref dxgiModes[i], out result[i]);

                return result;
            }

            public override void GetClosestMatchingMode(Device device, ref DisplayMode preferredMode, out DisplayMode result)
            {
                var d3d11Device = (device as SdxDevice).D3D11Device;

                DXGIModeDescription dxgiModeToMatch;
                ToDXGIModeDescription(ref preferredMode, out dxgiModeToMatch);

                DXGIModeDescription dxgiResult;
                dxgiOutput.GetClosestMatchingMode(d3d11Device, dxgiModeToMatch, out dxgiResult);

                FromDXGIModeDescription(ref dxgiResult, out result);
            }

            void FromDXGIModeDescription(ref DXGIModeDescription dxgiMode, out DisplayMode result)
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

            void ToDXGIModeDescription(ref DisplayMode mode, out DXGIModeDescription result)
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

        #endregion

        string description;

        int vendorId;

        int deviceId;

        int subSystemId;

        int revision;

        Output primaryOutput;

        public override string Description
        {
            get { return description; }
        }

        public override int VendorId
        {
            get { return vendorId; }
        }

        public override int DeviceId
        {
            get { return deviceId; }
        }

        public override int SubSystemId
        {
            get { return subSystemId; }
        }

        public override int Revision
        {
            get { return revision; }
        }

        public override Output PrimaryOutput
        {
            get { return primaryOutput; }
        }

        public DXGIAdapter1 DXGIAdapter { get; private set; }

        public SdxAdapter(bool isDefaultAdapter, DXGIAdapter1 dxgiAdapter)
            : base(isDefaultAdapter)
        {
            DXGIAdapter = dxgiAdapter;

            var adapterDescription = dxgiAdapter.Description1;

            description = adapterDescription.Description;
            vendorId = adapterDescription.VendorId;
            deviceId = adapterDescription.DeviceId;
            subSystemId = adapterDescription.SubsystemId;
            revision = adapterDescription.Revision;

            for (int i = 0; i < dxgiAdapter.Outputs.Length; i++)
            {
                var output = new SdxOutput(dxgiAdapter.Outputs[i]);
                AddOutput(output);
            }

            if (0 < dxgiAdapter.Outputs.Length)
                primaryOutput = Outputs[0];
        }

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                DXGIAdapter.Dispose();
            }

            base.DisposeOverride(disposing);
        }
    }
}
