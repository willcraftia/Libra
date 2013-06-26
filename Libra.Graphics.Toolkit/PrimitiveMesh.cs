#region Using

using System;

#endregion

// Xbox LIVE Indie Games - Primitives3D より移植
// http://xbox.create.msdn.com/en-US/education/catalog/sample/primitives_3d

namespace Libra.Graphics.Toolkit
{
    public abstract class PrimitiveMesh : IDisposable
    {
        VertexPositionNormal[] vertices;

        ushort[] indices;

        int currentVertexCount;

        int currentIndexCount;

        public DeviceContext DeviceContext { get; private set; }

        public VertexBuffer VertexBuffer { get; private set; }

        public IndexBuffer IndexBuffer { get; private set; }

        protected int CurrentVertex
        {
            get { return currentVertexCount; }
        }

        protected PrimitiveMesh(DeviceContext deviceContext)
        {
            if (deviceContext == null) throw new ArgumentNullException("deviceContext");

            DeviceContext = deviceContext;
        }

        public void Draw()
        {
            DeviceContext.SetVertexBuffer(VertexBuffer);
            DeviceContext.IndexBuffer = IndexBuffer;
            DeviceContext.PrimitiveTopology = PrimitiveTopology.TriangleList;

            DeviceContext.DrawIndexed(IndexBuffer.IndexCount);
        }

        protected void Allocate(int vertexCount, int indexCount)
        {
            vertices = new VertexPositionNormal[vertexCount];
            indices = new ushort[indexCount];
        }

        protected void AddVertex(Vector3 position, Vector3 normal)
        {
            vertices[currentVertexCount++] = new VertexPositionNormal(position, normal);
        }

        protected void AddIndex(int index)
        {
            if (ushort.MaxValue < (uint) index) throw new ArgumentOutOfRangeException("index");

            indices[currentIndexCount++] = (ushort) index;
        }

        protected void Build()
        {
            VertexBuffer = DeviceContext.Device.CreateVertexBuffer();
            VertexBuffer.Usage = ResourceUsage.Immutable;
            VertexBuffer.Initialize(vertices);

            IndexBuffer = DeviceContext.Device.CreateIndexBuffer();
            IndexBuffer.Format = IndexFormat.SixteenBits;
            IndexBuffer.Usage = ResourceUsage.Immutable;
            IndexBuffer.Initialize(indices);
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool disposed;

        ~PrimitiveMesh()
        {
            Dispose(false);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (VertexBuffer != null) VertexBuffer.Dispose();
            if (IndexBuffer != null) IndexBuffer.Dispose();

            disposed = true;
        }

        #endregion
    }
}
