#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public interface IPostprocess
    {
        bool Enabled { get; }

        void Apply(DeviceContext context);
    }
}
