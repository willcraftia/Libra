#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public interface IPostprocessor
    {
        bool Enabled { get; }

        ShaderResourceView Texture { set; }

        void Apply(DeviceContext context);
    }
}
