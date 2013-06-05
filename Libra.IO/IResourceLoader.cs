#region Using

using System;

#endregion

namespace Libra.IO
{
    public interface IResourceLoader
    {
        IResource Load(string uri);
    }
}
