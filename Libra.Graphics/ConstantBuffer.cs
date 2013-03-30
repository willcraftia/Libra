#region Using

using System;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    public abstract class ConstantBuffer : Resource
    {
        bool initialized;

        protected ConstantBuffer(IDevice device)
            : base(device)
        {
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

            InitializeCore(byteWidth);

            initialized = true;
        }

        public void Initialize<T>(int byteWidth, T data) where T : struct
        {
            AssertNotInitialized();
            if (byteWidth < 1) throw new ArgumentOutOfRangeException("byteWidth");
            if ((byteWidth % 16) != 0) throw new ArgumentException("byteWidth must be a multiple of 16", "byteWidth");

            InitializeCore<T>(byteWidth, data);

            initialized = true;
        }

        public void GetData<T>(DeviceContext context, out T data) where T : struct
        {
            AssertInitialized();
            if (context == null) throw new ArgumentNullException("context");

            GetDataCore(context, out data);
        }

        public void SetData<T>(DeviceContext context, T data) where T : struct
        {
            AssertInitialized();
            if (context == null) throw new ArgumentNullException("context");
            if (Usage == ResourceUsage.Immutable)
                throw new InvalidOperationException("Data can not be set from CPU.");

            var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var sourcePointer = gcHandle.AddrOfPinnedObject();
                var sizeInBytes = Marshal.SizeOf(typeof(T));

                unsafe
                {
                    if (Usage == ResourceUsage.Default)
                    {
                        context.UpdateSubresource(this, 0, null, sourcePointer, sizeInBytes, 0);
                    }
                    else
                    {
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
            }
            finally
            {
                gcHandle.Free();
            }
        }

        protected abstract void InitializeCore(int byteWidth);

        protected abstract void InitializeCore<T>(int byteWidth, T data) where T : struct;

        protected abstract void GetDataCore<T>(DeviceContext context, out T data) where T : struct;

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
