#region Using

using System;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    public abstract class VertexBuffer : Resource
    {
        bool initialized;

        public VertexDeclaration VertexDeclaration { get; private set; }

        public int VertexCount { get; private set; }

        protected VertexBuffer(IDevice device)
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

            InitializeCore();

            initialized = true;
        }

        public void Initialize<T>(VertexDeclaration vertexDeclaration, T[] data) where T : struct
        {
            AssertNotInitialized();
            if (vertexDeclaration == null) throw new ArgumentNullException("vertexDeclaration");
            if (data.Length == 0) throw new ArgumentException("Data must be not empty.", "data");

            VertexDeclaration = vertexDeclaration;
            VertexCount = Marshal.SizeOf(typeof(T)) * data.Length / vertexDeclaration.Stride;

            InitializeCore(data);

            initialized = true;
        }

        public void Initialize<T>(T[] data) where T : struct, IVertexType
        {
            AssertNotInitialized();
            if (data.Length == 0) throw new ArgumentException("Data must be not empty.", "data");

            VertexDeclaration = data[0].VertexDeclaration;
            VertexCount = data.Length;

            InitializeCore(data);

            initialized = true;
        }

        public void GetData<T>(DeviceContext context, T[] data, int startIndex, int elementCount) where T : struct
        {
            AssertInitialized();
            if (context == null) throw new ArgumentNullException("context");
            if (data == null) throw new ArgumentNullException("data");
            if (startIndex < 0) throw new ArgumentOutOfRangeException("startIndex");
            if (data.Length < (startIndex + elementCount)) throw new ArgumentOutOfRangeException("elementCount");

            GetDataCore(context, data, startIndex, elementCount);
        }

        public void GetData<T>(DeviceContext context, T[] data) where T : struct
        {
            GetData(context, data, 0, data.Length);
        }

        public void SetData<T>(DeviceContext context, T[] data, int startIndex, int elementCount) where T : struct
        {
            AssertInitialized();
            if (context == null) throw new ArgumentNullException("context");
            if (data == null) throw new ArgumentNullException("data");
            if (startIndex < 0) throw new ArgumentOutOfRangeException("startIndex");
            if (data.Length < (startIndex + elementCount)) throw new ArgumentOutOfRangeException("elementCount");

            if (Usage == ResourceUsage.Immutable)
                throw new InvalidOperationException("Data can not be set from CPU.");

            var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var dataPointer = gcHandle.AddrOfPinnedObject();
                var sizeOfT = Marshal.SizeOf(typeof(T));

                var sourcePointer = (IntPtr) (dataPointer + startIndex * sizeOfT);

                if (Usage == ResourceUsage.Default)
                {
                    context.UpdateSubresource(this, 0, null, sourcePointer, 0, 0);
                }
                else
                {
                    var sizeInBytes = ((elementCount == 0) ? data.Length : elementCount) * sizeOfT;

                    // TODO
                    //
                    // Dynamic だと D3D11MapMode.Write はエラーになる。
                    // 対応関係を MSDN から把握できないが、どうすべきか。
                    // ひとまず WriteDiscard とする。

                    var mappedResource = context.Map(this, 0, DeviceContext.MapMode.WriteDiscard);
                    try
                    {
                        GraphicsHelper.CopyMemory(mappedResource.Pointer, sourcePointer, sizeInBytes);
                    }
                    finally
                    {
                        context.Unmap(this, 0);
                    }
                }
            }
            finally
            {
                gcHandle.Free();
            }
        }

        public void SetData<T>(DeviceContext context, T[] data) where T : struct
        {
            SetData(context, data, 0, data.Length);
        }

        public void SetData<T>(DeviceContext context, T[] data, int sourceIndex, int elementCount,
            SetDataOptions options = SetDataOptions.None) where T : struct
        {
            SetData(context, 0, data, sourceIndex, elementCount, options);
        }

        public void SetData<T>(DeviceContext context, int offsetInBytes, T[] data, int sourceIndex, int elementCount,
            SetDataOptions options = SetDataOptions.None) where T : struct
        {
            AssertInitialized();
            if (context == null) throw new ArgumentNullException("context");
            if (data == null) throw new ArgumentNullException("data");
            if (sourceIndex < 0) throw new ArgumentOutOfRangeException("startIndex");
            if (data.Length < (sourceIndex + elementCount)) throw new ArgumentOutOfRangeException("elementCount");
            
            if (Usage != ResourceUsage.Dynamic) throw new InvalidOperationException("Resource not writable.");

            if (options == SetDataOptions.Discard && Usage != ResourceUsage.Dynamic)
                throw new InvalidOperationException("Resource.Usage must be dynamic for discard option.");

            var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var dataPointer = gcHandle.AddrOfPinnedObject();
                var sizeOfT = Marshal.SizeOf(typeof(T));

                var sourcePointer = (IntPtr) (dataPointer + sourceIndex * sizeOfT);
                var sizeInBytes = ((elementCount == 0) ? data.Length : elementCount) * sizeOfT;

                // メモ
                //
                // D3D11MapFlags.DoNotWait は、Discard と NoOverwite では使えない。
                // D3D11MapFlags 参照のこと。

                var mappedResource = context.Map(this, 0, (DeviceContext.MapMode) options);
                var destinationPointer = (IntPtr) (mappedResource.Pointer + offsetInBytes);

                try
                {
                    GraphicsHelper.CopyMemory(destinationPointer, sourcePointer, sizeInBytes);
                }
                finally
                {
                    // Unmap
                    context.Unmap(this, 0);
                }
            }
            finally
            {
                gcHandle.Free();
            }
        }

        protected abstract void InitializeCore();

        protected abstract void InitializeCore<T>(T[] data) where T : struct;

        protected abstract void GetDataCore<T>(DeviceContext context, T[] data, int startIndex, int elementCount) where T : struct;

        void AssertNotInitialized()
        {
            if (initialized) throw new InvalidOperationException("Already initialized.");
        }

        void AssertInitialized()
        {
            if (!initialized) throw new InvalidOperationException("Not initialized.");
        }
    }
}
