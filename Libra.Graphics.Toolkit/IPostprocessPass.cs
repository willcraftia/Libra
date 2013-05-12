#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public interface IPostprocessPass
    {
        bool Enabled { get; }

        void Apply(DeviceContext context);
    }
}
