#region Using

using System;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    public abstract class ConstantBuffer : Resource
    {
        bool initialized;

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

            initialized = true;
        }

        public void Initialize<T>(int byteWidth, T data) where T : struct
        {
            AssertNotInitialized();
            if (byteWidth < 1) throw new ArgumentOutOfRangeException("byteWidth");
            if ((byteWidth % 16) != 0) throw new ArgumentException("byteWidth must be a multiple of 16", "byteWidth");

            ByteWidth = byteWidth;

            InitializeCore<T>(data);

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

            // 配列を含んだ構造体を扱うために必要な処理。
            // Marshal.AllocHGlobal でアンマネージ領域にメモリを確保し、
            // そこへ Marshal.StructureToPtr で構造体データを配置。
            // この領域を更新元データとして UpdateSubresource で更新する、あるいは、
            // Map されたリソースへコピーする。

            var sourcePointer = Marshal.AllocHGlobal(ByteWidth);
            try
            {
                Marshal.StructureToPtr(data, sourcePointer, false);

                unsafe
                {
                    if (Usage == ResourceUsage.Default)
                    {
                        context.UpdateSubresource(this, 0, null, sourcePointer, ByteWidth, 0);
                    }
                    else
                    {
                        var mappedResource = context.Map(this, 0, DeviceContext.MapMode.WriteDiscard);
                        try
                        {
                            GraphicsHelper.CopyMemory(mappedResource.Pointer, sourcePointer, ByteWidth);
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
                Marshal.FreeHGlobal(sourcePointer);
            }
        }

        protected abstract void InitializeCore();

        protected abstract void InitializeCore<T>(T data) where T : struct;

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
