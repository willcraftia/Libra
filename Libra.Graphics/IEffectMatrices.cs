#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public interface IEffectMatrices
    {
        Matrix World { get; set; }

        Matrix View { get; set; }

        Matrix Projection { get; set; }
    }
}
