#region Using

using System;
using System.Diagnostics;

#endregion

namespace Libra.Games
{
    public sealed class GameTimer
    {
        long lastRawTime;
        
        public TimeSpan ElapsedTime { get; private set; }

        public void Reset()
        {
            lastRawTime = Stopwatch.GetTimestamp();
        }

        public void Tick()
        {
            var rawTime = Stopwatch.GetTimestamp();
            ElapsedTime = ConvertRawToTimestamp(rawTime - lastRawTime);

            if (ElapsedTime < TimeSpan.Zero)
            {
                ElapsedTime = TimeSpan.Zero;
            }

            lastRawTime = rawTime;
        }

        static TimeSpan ConvertRawToTimestamp(long delta)
        {
            return TimeSpan.FromTicks((delta * 10000000) / Stopwatch.Frequency);
        }
    }
}
