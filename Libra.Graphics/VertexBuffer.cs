#region Using

using System;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    public abstract class VertexBuffer : Resource
    {
        public VertexDeclaration VertexDeclaration { get; private set; }

        public int VertexCount { get; private set; }

        public int ByteWidth { get; private set; }

        public bool Initialized { get; protected internal set; }

        protected VertexBuffer(Device device)
            : base(device)
        {
        }

        public void Initialize(VertexDeclaration vertexDeclaration, int vertexCount)
        {
            AssertNotInitialized();
            if (vertexDeclaration == null) throw new ArgumentNullException("vertexDeclaration");
            if (vertexCount < 1) throw new ArgumentOutOfRangeException("vertexCount");
            if (Usage == ResourceUsage.Immutable) throw new InvalidOperationException("Usage must be not immutable.");

            VertexDeclaration = vertexDeclaration;
            VertexCount = vertexCount;
            ByteWidth = VertexDeclaration.Stride * VertexCount;

            InitializeCore();

            Initialized = true;
        }

        public void Initialize<T>(VertexDeclaration vertexDeclaration, T[] data) where T : struct
        {
            AssertNotInitialized();
            if (vertexDeclaration == null) throw new ArgumentNullException("vertexDeclaration");
            if (data.Length == 0) throw new ArgumentException("Data must be not empty.", "data");

            VertexDeclaration = vertexDeclaration;
            VertexCount = Marshal.SizeOf(typeof(T)) * data.Length / vertexDeclaration.Stride;
            ByteWidth = VertexDeclaration.Stride * VertexCount;

            InitializeCore(data);

            Initialized = true;
        }

        public void Initialize<T>(int vertexCount) where T : struct, IVertexType
        {
            Initialize(new T().VertexDeclaration, vertexCount);
        }

        public void Initialize<T>(T[] data) where T : struct, IVertexType
        {
            if (data.Length == 0) throw new ArgumentException("Data must be not empty.", "data");

            Initialize<T>(data[0].VertexDeclaration, data);
        }

        protected abstract void InitializeCore();

        protected abstract void InitializeCore<T>(T[] data) where T : struct;

        void AssertNotInitialized()
        {
            if (Initialized) throw new InvalidOperationException("Already initialized.");
        }
    }
}
