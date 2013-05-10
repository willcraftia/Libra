#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public interface IPostprocessor : IEffect
    {
        bool Enabled { get; }

        ShaderResourceView Texture { set; }
    }
}
