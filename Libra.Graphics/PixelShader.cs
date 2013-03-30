#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public abstract class PixelShader : Shader
    {
        protected PixelShader(IDevice device)
            : base(device)
        {
        }
    }
}
