#region Using

using System;
using System.IO;

#endregion

namespace Libra.IO
{
    public sealed class FileResource : IResource, IEquatable<FileResource>
    {
        public const string FileScheme = "file";

        string extension;

        object extensionLock = new object();

        string baseUri;

        object baseUriLock = new object();

        public string AbsoluteUri { get; internal set; }

        public string Scheme
        {
            get { return FileScheme; }
        }

        public string AbsolutePath { get; internal set; }

        public string Extension
        {
            get
            {
                lock (extensionLock)
                {
                    if (extension == null)
                        extension = Path.GetExtension(AbsolutePath);
                    return extension;
                }
            }
        }

        public bool ReadOnly
        {
            get
            {
                if (!Exists)
                {
                    return false;
                }

                var attributes = File.GetAttributes(AbsolutePath);
                return (attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
            }
        }

        public bool Exists
        {
            get
            {
                return File.Exists(AbsolutePath);
            }
        }

        public string BaseUri
        {
            get
            {
                lock (baseUriLock)
                {
                    if (baseUri == null)
                    {
                        var lastSlash = AbsolutePath.LastIndexOf('/');
                        if (lastSlash < 0) lastSlash = 0;
                        var basePath = AbsolutePath.Substring(0, lastSlash + 1);
                        baseUri = FileScheme + ":///" + basePath;
                    }
                    return baseUri;
                }
            }
        }

        internal FileResource() { }

        public Stream OpenRead()
        {
            return File.OpenRead(AbsolutePath);
        }

        public Stream OpenWrite()
        {
            if (ReadOnly) throw new IOException("The read-only resource can not be written.");

            return File.OpenWrite(AbsolutePath);
        }

        public Stream CreateNew()
        {
            if (ReadOnly) throw new IOException("The read-only resource can not be created.");

            return File.Open(AbsolutePath, FileMode.Create);
        }

        public void Delete()
        {
            if (ReadOnly) throw new IOException("The read-only resource can not be deleted.");

            File.Delete(AbsolutePath);
        }

        #region Equatable

        public bool Equals(FileResource other)
        {
            return AbsoluteUri == other.AbsoluteUri;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((FileResource) obj);
        }

        public override int GetHashCode()
        {
            return AbsoluteUri.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return AbsoluteUri;
        }

        #endregion
    }
}
