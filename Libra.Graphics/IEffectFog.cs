#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public interface IEffectFog
    {
        bool FogEnabled { get; set; }

        float FogStart { get; set; }

        float FogEnd { get; set; }

        Vector3 FogColor { get; set; }
    }
}
