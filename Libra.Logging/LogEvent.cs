#region Using

using System;

#endregion

namespace Libra.Logging
{
    public struct LogEvent
    {
        public LogLevel Level;

        public string Category;

        public DateTime DateTime;

        public int ThreadId;

        public string Message;

        public Exception Exception;
    }
}
