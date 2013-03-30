#region Using

using System;

using SDXTimerTick = SharpDX.TimerTick;

#endregion

namespace Libra.Games.SharpDX
{
    public sealed class SdxGameTimer : IGameTimer
    {
        SDXTimerTick timer;

        public TimeSpan ElapsedTime
        {
            get { return timer.ElapsedAdjustedTime; }
        }

        public SdxGameTimer()
        {
            timer = new SDXTimerTick();
        }

        public void Initialize()
        {
            timer.Reset();
        }

        public void Tick()
        {
            timer.Tick();
        }

        public void Reset()
        {
            timer.Reset();
        }
    }
}
