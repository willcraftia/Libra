#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public sealed class BilateralFilterPass : IFilterEffect
    {
        public BilateralFilter BilateralFilter { get; private set; }

        public GaussianFilterDirection Direction { get; private set; }

        public bool Enabled { get; set; }

        public BilateralFilterPass(BilateralFilter bilateralFilter, GaussianFilterDirection direction)
        {
            if (bilateralFilter == null) throw new ArgumentNullException("bilateralFilter");

            BilateralFilter = bilateralFilter;
            Direction = direction;

            Enabled = true;
        }

        public void Apply(DeviceContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            BilateralFilter.Direction = Direction;
            BilateralFilter.Apply(context);
        }
    }
}
