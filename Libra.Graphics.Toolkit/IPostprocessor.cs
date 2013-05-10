#region Using

using System;

#endregion

namespace Libra.Graphics.Toolkit
{
    public interface IPostprocessor : IEffect
    {
        ShaderResourceView Texture { set; }
    }
}
