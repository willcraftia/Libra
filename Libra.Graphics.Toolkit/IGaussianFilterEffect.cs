#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public interface IGaussianFilterEffect : IFilterEffect
    {
        GaussianFilterDirection Direction { get; set; }
    }
}
