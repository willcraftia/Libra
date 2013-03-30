#region Using

using System;
using Libra.Graphics;

#endregion

namespace Libra.Xnb
{
    public sealed class XnbManager : IDisposable
    {
        Felis.Xnb.ContentManager entity;

        public string RootDirectory
        {
            get { return entity.RootDirectory; }
            set { entity.RootDirectory = value; }
        }

        public XnbManager(IServiceProvider serviceProvider)
            : this(serviceProvider, string.Empty)
        {
        }

        public XnbManager(IServiceProvider serviceProvider, string rootDirectory)
        {
            if (serviceProvider == null) throw new ArgumentNullException("serviceProvider");
            if (rootDirectory == null) throw new ArgumentNullException("rootDirectory");

            entity = new Felis.Xnb.ContentManager(serviceProvider, rootDirectory);
            InitializeEntity();
        }

        void InitializeEntity()
        {
            entity.TypeReaderManager.RegisterStandardTypeReaders();

            RegisterTypeBuilder<Vector3Builder>();
            RegisterTypeBuilder<RectangleBuilder>();
            RegisterTypeBuilder<MatrixBuilder>();
            RegisterTypeBuilder<BoundingSphereBuilder>();
            RegisterTypeBuilder<VertexBufferBuilder>();
            RegisterTypeBuilder<VertexDeclarationBuilder>();
            RegisterTypeBuilder<IndexBufferBuilder>();
            RegisterTypeBuilder<BasicEffectBuilder>();
            RegisterTypeBuilder<ModelBuilder>();
            RegisterTypeBuilder<Texture2DBuilder>();
            RegisterTypeBuilder<SpriteFontBuilder>();
        }

        void RegisterTypeBuilder<T>() where T : Felis.Xnb.TypeBuilder, new()
        {
            entity.TypeReaderManager.RegisterTypeBuilder<T>();
        }

        public T Load<T>(string assetName, DeviceContext context = null)
        {
            return entity.Load<T>(assetName, context);
        }

        public void Unload()
        {
            entity.Unload();
        }

        #region IDisposable

        bool disposed;

        ~XnbManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                Unload();
            }

            disposed = true;
        }

        #endregion
    }
}
