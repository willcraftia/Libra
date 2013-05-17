#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public interface IFilterEffect
    {
        bool Enabled { get; }

        void Apply(DeviceContext context);
    }
}
