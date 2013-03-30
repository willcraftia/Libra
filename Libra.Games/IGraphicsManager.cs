#region Using

using System;

#endregion

namespace Libra.Games
{
    public interface IGraphicsManager
    {
        void Initialize();

        bool BeginDraw();

        void EndDraw();
    }
}
