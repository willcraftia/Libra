#region Using

using System;
using System.Diagnostics;

#endregion

namespace Libra.Logging
{
    public sealed class TraceAppender : IAppender
    {
        public static TraceAppender Instance = new TraceAppender();

        static readonly string[] levelStrings =
        {
            "FATAL", "ERROR", "WARN", "INFO", "DEBUG"
        };

        TraceAppender() { }

        public void Append(ref LogEvent logEvent)
        {
            lock (this)
            {
                Trace.Write(logEvent.DateTime.ToString("HH:mm:ss.fffffff"));
                Trace.Write(string.Format(" [{0,2}] ", logEvent.ThreadId));
                Trace.Write(string.Format("{0,-5} ", levelStrings[(int) logEvent.Level]));
                Trace.Write(" ");
                Trace.Write(logEvent.Category);
                Trace.Write(" - ");
                Trace.WriteLine(logEvent.Message);

                if (logEvent.Exception != null) AppendException(logEvent.Exception);

                Trace.Flush();
            }
        }

        void AppendException(Exception exception)
        {
            Trace.Write(exception.GetType().FullName);
            Trace.Write(": ");
            Trace.WriteLine(exception.Message);
            Trace.WriteLine(exception.StackTrace);

            if (exception.InnerException != null) AppendException(exception.InnerException);
        }
    }
}
