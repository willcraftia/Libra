#region Using

using System;

#endregion

namespace Libra.Games
{
    public sealed class GameTime
    {
        TimeSpan elapsedGameTime;
        
        bool isRunningSlowly;
        
        TimeSpan totalGameTime;

        public GameTime() { }

        public GameTime(TimeSpan totalGameTime, TimeSpan elapsedGameTime)
            : this(totalGameTime, elapsedGameTime, false)
        {
        }

        public GameTime(TimeSpan totalGameTime, TimeSpan elapsedGameTime, bool isRunningSlowly)
        {
            this.totalGameTime = totalGameTime;
            this.elapsedGameTime = elapsedGameTime;
            this.isRunningSlowly = isRunningSlowly;
        }

        public TimeSpan ElapsedGameTime
        {
            get { return elapsedGameTime; }
            internal set { elapsedGameTime = value; }
        }

        public bool IsRunningSlowly
        {
            get { return isRunningSlowly; }
            internal set { isRunningSlowly = value; }
        }

        public TimeSpan TotalGameTime
        {
            get { return totalGameTime; }
            internal set { totalGameTime = value; }
        }
    }
}
