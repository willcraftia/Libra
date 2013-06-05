#region Using

using System;
using System.IO;

#endregion

namespace Libra.IO
{
    public interface IResource
    {
        string AbsoluteUri { get; }

        string Scheme { get; }

        string AbsolutePath { get; }

        string Extension { get; }

        bool ReadOnly { get; }

        string BaseUri { get; }

        Stream Open();

        Stream Create();

        void Delete();
    }
}
