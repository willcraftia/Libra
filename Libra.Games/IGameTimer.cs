#region Using

using System;

#endregion

namespace Libra.Games
{
    public interface IGameTimer
    {
        TimeSpan ElapsedTime { get; }

        void Initialize();

        void Tick();

        void Reset();
    }
}
