#region Using

using System;

#endregion

namespace Libra.Logging
{
    public interface IAppender
    {
        void Append(ref LogEvent logEvent);
    }
}
