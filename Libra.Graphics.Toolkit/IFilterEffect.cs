#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public interface IFilterEffect : IEffect
    {
        bool Enabled { get; }
    }
}
