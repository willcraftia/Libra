#region Using

using System;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    public abstract class IndexBuffer : Resource
    {
        bool initialized;

        IndexFormat format;

        public IndexFormat Format
        {
            get { return format; }
            set
            {
                AssertNotInitialized();

                format = value;
            }
        }

        public int IndexCount { get; private set; }

        protected IndexBuffer(IDevice device)
            : base(device)
        {
            format = IndexFormat.SixteenBits;
        }

        public void Initialize(int indexCount)
        {
            AssertNotInitialized();
            if (indexCount < 1) throw new ArgumentOutOfRangeException("indexCount");

            if (Usage == ResourceUsage.Immutable)
                throw new InvalidOperationException("Usage must be not immutable.");

            IndexCount = indexCount;

            InitializeCore();

            initialized = true;
        }

        public void Initialize<T>(T[] data) where T : struct
        {
            AssertNotInitialized();
            if (data == null) throw new ArgumentNullException("data");
            if (data.Length == 0) throw new ArgumentException("Data must be not empty.", "data");

            var sizeOfT = Marshal.SizeOf(typeof(T));
            var totalSizeInBytes = sizeOfT * data.Length;

            IndexCount = totalSizeInBytes / FormatHelper.SizeInBytes(Format);

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
            AssertInitialized();
            if (context == null) throw new ArgumentNullException("context");
            if (data == null) throw new ArgumentNullException("data");

            GetDataCore(context, data, 0, data.Length);
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

        public void SetData<T>(DeviceContext context, params T[] data) where T : struct
        {
            SetData(context, data, 0, data.Length);
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
