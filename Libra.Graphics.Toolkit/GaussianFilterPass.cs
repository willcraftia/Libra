#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    /// <summary>
    /// ガウシアン フィルタと適用方向の組をフィルタ オブジェクトとするアダプタです。
    /// </summary>
    public sealed class GaussianFilterPass : IFilterEffect
    {
        public IGaussianFilterEffect GaussianFilterEffect { get; private set; }

        public GaussianFilterDirection Direction { get; private set; }

        public bool Enabled { get; set; }

        public ShaderResourceView Texture { get; set; }

        public SamplerState TextureSampler { get; set; }

        public GaussianFilterPass(IGaussianFilterEffect gaussianFilterEffect, GaussianFilterDirection direction)
        {
            if (gaussianFilterEffect == null) throw new ArgumentNullException("gaussianFilterEffect");

            GaussianFilterEffect = gaussianFilterEffect;
            Direction = direction;

            Enabled = true;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            GaussianFilterEffect.Direction = Direction;
            GaussianFilterEffect.Texture = Texture;
            GaussianFilterEffect.TextureSampler = TextureSampler;
            GaussianFilterEffect.Apply(context);
        }
    }
}
