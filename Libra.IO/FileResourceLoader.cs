#region Using

using System;

#endregion

namespace Libra.IO
{
    public sealed class FileResourceLoader : IResourceLoader
    {
        const string prefix = "file:///";

        public IResource Load(string uri)
        {
            if (!uri.StartsWith(prefix)) return null;

            var absolutePath = uri.Substring(prefix.Length);
            return new FileResource { AbsoluteUri = uri, AbsolutePath = absolutePath };
        }
    }
}
