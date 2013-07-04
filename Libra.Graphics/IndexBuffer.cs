#region Using

using System;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    public abstract class IndexBuffer : Resource
    {
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

        public int ByteWidth { get; private set; }

        public bool Initialized { get; protected internal set; }

        protected IndexBuffer(Device device)
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
            ByteWidth = FormatHelper.SizeInBytes(Format) * IndexCount;

            InitializeCore();

            Initialized = true;
        }

        public void Initialize<T>(T[] data) where T : struct
        {
            AssertNotInitialized();
            if (data == null) throw new ArgumentNullException("data");
            if (data.Length == 0) throw new ArgumentException("Data must be not empty.", "data");

            var sizeOfT = Marshal.SizeOf(typeof(T));
            var totalSizeInBytes = sizeOfT * data.Length;

            IndexCount = totalSizeInBytes / FormatHelper.SizeInBytes(Format);
            ByteWidth = FormatHelper.SizeInBytes(Format) * IndexCount;

            InitializeCore(data);

            Initialized = true;
        }

        protected abstract void InitializeCore();

        protected abstract void InitializeCore<T>(T[] data) where T : struct;

        void AssertNotInitialized()
        {
            if (Initialized) throw new InvalidOperationException("Already initialized.");
        }
    }
}
