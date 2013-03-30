#region Using

using System;
using System.Collections.Generic;

using DXGIFormat = SharpDX.DXGI.Format;
using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11Feature = SharpDX.Direct3D11.Feature;
using D3D11FormatSupport = SharpDX.Direct3D11.FormatSupport;
using D3D11FormatSupport2 = SharpDX.Direct3D11.ComputeShaderFormatSupport;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxDeviceFeatures
    {
        #region FormatFeature

        sealed class FormatFeature
        {
            public D3D11FormatSupport D3D11FormatSupport;

            public D3D11FormatSupport2 D3D11FormatSupport2;

            public int MaxMultiSampleCount;
        }

        #endregion

        internal static SdxDeviceFeatures Create(D3D11Device device)
        {
            var instance = new SdxDeviceFeatures();

            instance.Doubles = device.CheckFeatureSupport(D3D11Feature.ShaderDoubles);

            bool threadingDriverConcurrentCreates;
            bool threadingDriverCommandLists;
            device.CheckThreadingSupport(out threadingDriverConcurrentCreates, out threadingDriverCommandLists);
            instance.ThreadingDriverConcurrentCreates = threadingDriverConcurrentCreates;
            instance.ThreadingDriverCommandLists = threadingDriverCommandLists;

            // SurfaceFormat について。
            foreach (var format in FormatHelper.DXGIFormatsAsSurfaceFormat)
                instance.formatFeatures[format] = CreateFormatFeature(device, format);

            // DepthFormat について。
            foreach (var format in FormatHelper.DXGIFormatsAsDepthFormat)
                instance.formatFeatures[format] = CreateFormatFeature(device, format);

            return instance;
        }

        static FormatFeature CreateFormatFeature(D3D11Device device, DXGIFormat format)
        {
            var instance = new FormatFeature();

            instance.D3D11FormatSupport = device.CheckFormatSupport(format);
            instance.D3D11FormatSupport2 = device.CheckComputeShaderFormatSupport(format);

            instance.MaxMultiSampleCount = 1;
            for (int i = 1; i <= 8; i *= 2)
            {
                if (device.CheckMultisampleQualityLevels(format, i) != 0)
                    instance.MaxMultiSampleCount = i;
            }

            return instance;
        }

        Dictionary<DXGIFormat, FormatFeature> formatFeatures;

        public bool Doubles { get; private set; }

        public bool ThreadingDriverConcurrentCreates { get; private set; }

        public bool ThreadingDriverCommandLists { get; private set; }

        SdxDeviceFeatures()
        {
            formatFeatures = new Dictionary<DXGIFormat, FormatFeature>();
        }
    }
}
