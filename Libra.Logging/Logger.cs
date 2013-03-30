#region Using

using System;
using System.Diagnostics;
using System.Threading;

#endregion

namespace Libra.Logging
{
    public sealed class Logger
    {
        public static IAppender Appender { get; private set; }

        string category;

        static Logger()
        {
            Appender = TraceAppender.Instance;
        }

        public Logger(string category)
        {
            this.category = category;
        }

        [Conditional("TRACE"), Conditional("DEBUG")]
        public static void Initialize(IAppender appender)
        {
            Appender = appender;
        }

        [Conditional("TRACE")]
        public void Fatal(string message, params object[] arg)
        {
            Log(LogLevel.Fatal, null, message, arg);
        }

        [Conditional("TRACE")]
        public void Error(string message, params object[] arg)
        {
            Log(LogLevel.Error, null, message, arg);
        }

        [Conditional("TRACE")]
        public void Warn(string message, params object[] arg)
        {
            Log(LogLevel.Warn, null, message, arg);
        }

        [Conditional("TRACE")]
        public void Info(string message, params object[] arg)
        {
            Log(LogLevel.Info, null, message, arg);
        }

        [Conditional("DEBUG")]
        public void Debug(string message, params object[] arg)
        {
            Log(LogLevel.Debug, null, message, arg);
        }

        [Conditional("TRACE")]
        public void Fatal(Exception exception, string message, params object[] arg)
        {
            Log(LogLevel.Fatal, exception, message, arg);
        }

        [Conditional("TRACE")]
        public void Error(Exception exception, string message, params object[] arg)
        {
            Log(LogLevel.Error, exception, message, arg);
        }

        [Conditional("TRACE"), Conditional("DEBUG")]
        void Log(LogLevel level, Exception exception, string message, params object[] arg)
        {
            if (Appender == null) return;

            if (arg != null && arg.Length != 0)
                message = string.Format(message, arg);

            var logEvent = new LogEvent
            {
                Level = level,
                Category = category,
                DateTime = DateTime.Now,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                Message = message,
                Exception = exception
            };
            Appender.Append(ref logEvent);
        }

        //====================================================================
        // Utilitiy Methods
        //

        [Conditional("TRACE")]
        public void InfoGameStarted()
        {
            Info("<START> " + DateTime.Now.ToString("yyyy/MM/dd"));
        }

        [Conditional("TRACE")]
        public void InfoGameExited()
        {
            Info("<EXIT > " + DateTime.Now.ToString("yyyy/MM/dd"));
        }

        [Conditional("TRACE")]
        public void InfoBegin(string message, params object[] arg)
        {
            if (arg.Length == 0)
            {
                Info("<Begin> " + message);
            }
            else
            {
                Info("<Begin> " + string.Format(message, arg));
            }
        }

        [Conditional("TRACE")]
        public void InfoEnd(string message, params object[] arg)
        {
            if (arg.Length == 0)
            {
                Info("<End  > " + message);
            }
            else
            {
                Info("<End  > " + string.Format(message, arg));
            }
        }

        [Conditional("TRACE")]
        public void InfoIf(bool condition, string message, params object[] arg)
        {
            if (condition)
                Info(message, arg);
        }
    }
}
