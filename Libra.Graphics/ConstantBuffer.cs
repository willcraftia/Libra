#region Using

using System;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    public abstract class ConstantBuffer : Resource
    {
        protected internal bool Initialized { get; set; }

        public int ByteWidth { get; private set; }

        protected ConstantBuffer(Device device)
            : base(device)
        {
            Usage = ResourceUsage.Dynamic;
        }

        public void Initialize<T>() where T : struct
        {
            Initialize(Marshal.SizeOf(typeof(T)));
        }

        public void Initialize<T>(T data) where T : struct
        {
            Initialize<T>(Marshal.SizeOf(typeof(T)), data);
        }

        public void Initialize(int byteWidth)
        {
            AssertNotInitialized();
            if (byteWidth < 1) throw new ArgumentOutOfRangeException("byteWidth");
            if ((byteWidth % 16) != 0) throw new ArgumentException("byteWidth must be a multiple of 16", "byteWidth");
            if (Usage == ResourceUsage.Immutable)
                throw new InvalidOperationException("Usage must be not immutable.");

            ByteWidth = byteWidth;

            InitializeCore();

            Initialized = true;
        }

        public void Initialize<T>(int byteWidth, T data) where T : struct
        {
            AssertNotInitialized();
            if (byteWidth < 1) throw new ArgumentOutOfRangeException("byteWidth");
            if ((byteWidth % 16) != 0) throw new ArgumentException("byteWidth must be a multiple of 16", "byteWidth");

            ByteWidth = byteWidth;

            InitializeCore<T>(data);

            Initialized = true;
        }

        protected abstract void InitializeCore();

        protected abstract void InitializeCore<T>(T data) where T : struct;

        void AssertNotInitialized()
        {
            if (Initialized) throw new InvalidOperationException("Already initialized.");
        }
    }
}
