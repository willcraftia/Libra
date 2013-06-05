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
                var attributes = File.GetAttributes(AbsolutePath);
                return (attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
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

        public bool Exists()
        {
            return File.Exists(AbsolutePath);
        }

        public Stream Open()
        {
            return File.Open(AbsolutePath, FileMode.Open);
        }

        public Stream Create()
        {
            return File.Create(AbsolutePath);
        }

        public void Delete()
        {
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
