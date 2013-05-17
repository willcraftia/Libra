#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    /// <summary>
    /// ガウシアン フィルタと適用方向の組をフィルタ オブジェクトとするアダプタです。
    /// </summary>
    public sealed class GaussianFilter : IFilterEffect
    {
        public GaussianFilterCore Core { get; private set; }

        public GaussianFilterDirection Direction { get; private set; }

        public bool Enabled { get; set; }

        public GaussianFilter(GaussianFilterCore core, GaussianFilterDirection direction)
        {
            if (core == null) throw new ArgumentNullException("core");

            Core = core;
            Direction = direction;

            Enabled = true;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            Core.Direction = Direction;
            Core.Apply(context);
        }
    }
}
