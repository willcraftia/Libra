#region Using

using System;
using System.IO;

#endregion

namespace Libra
{
    public static class TitleContainer
    {
        static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        public static Stream OpenStream(string name)
        {
            var fullPath = Path.Combine(BaseDirectory, name);
            return File.OpenRead(fullPath);
        }
    }
}
