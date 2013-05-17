#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class BilateralFilter : IFilterEffect
    {
        public BilateralFilterCore Core { get; private set; }

        public GaussianFilterDirection Direction { get; private set; }

        public bool Enabled { get; set; }

        public BilateralFilter(BilateralFilterCore core, GaussianFilterDirection direction)
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
