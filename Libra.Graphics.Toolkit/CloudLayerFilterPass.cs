#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class CloudLayerFilterPass : IFilterEffect
    {
        Vector2 pixelOffset;

        public DeviceContext DeviceContext
        {
            get { return CloudLayerFilter.DeviceContext; }
        }

        public CloudLayerFilter CloudLayerFilter { get; set; }

        public Vector2 PixelOffset
        {
            get { return pixelOffset; }
            set { pixelOffset = value; }
        }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public CloudLayerFilterPass(CloudLayerFilter cloudLayerFilter)
        {
            if (cloudLayerFilter == null) throw new ArgumentNullException("cloudLayerFilter");

            CloudLayerFilter = cloudLayerFilter;

            Enabled = true;
        }

        public void Apply()
        {
            CloudLayerFilter.PixelOffset = pixelOffset;
            CloudLayerFilter.Texture = Texture;
            CloudLayerFilter.TextureSampler = TextureSampler;
            CloudLayerFilter.Apply();
        }
    }
}
