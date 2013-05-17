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
        public GaussianFilter GaussianFilter { get; private set; }

        public GaussianFilterDirection Direction { get; private set; }

        public bool Enabled { get; set; }

        public GaussianFilterPass(GaussianFilter gaussianFilter, GaussianFilterDirection direction)
        {
            if (gaussianFilter == null) throw new ArgumentNullException("gaussianFilter");

            GaussianFilter = gaussianFilter;
            Direction = direction;

            Enabled = true;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            GaussianFilter.Direction = Direction;
            GaussianFilter.Apply(context);
        }
    }
}
