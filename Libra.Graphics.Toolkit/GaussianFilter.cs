#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    /// <summary>
    /// ガウシアン フィルタとパスの組み合わせをフィルタ オブジェクトとするアダプタです。
    /// </summary>
    public sealed class GaussianFilter : IFilterEffect
    {
        public GaussianFilterCore Core { get; private set; }

        public GaussianFilterPass Pass { get; private set; }

        public bool Enabled { get; set; }

        public GaussianFilter(GaussianFilterCore core, GaussianFilterPass pass)
        {
            if (core == null) throw new ArgumentNullException("core");

            Core = core;
            Pass = pass;

            Enabled = true;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            Core.Pass = Pass;
            Core.Apply(context);
        }
    }
}
