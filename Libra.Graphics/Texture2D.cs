#region Using

using System;
using System.IO;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    public abstract class Texture2D : Resource
    {
        bool initialized;

        int width;

        int height;

        int mipLevels;

        SurfaceFormat format;

        int multisampleCount;

        public int Width
        {
            get { return width; }
            set
            {
                AssertNotInitialized();
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                width = value;
            }
        }

        public int Height
        {
            get { return height; }
            set
            {
                AssertNotInitialized();
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                height = value;
            }
        }

        public int MipLevels
        {
            get { return mipLevels; }
            set
            {
                AssertNotInitialized();
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                mipLevels = value;
            }
        }

        public SurfaceFormat Format
        {
            get { return format; }
            set
            {
                AssertNotInitialized();

                format = value;
            }
        }

        public int MultisampleCount
        {
            get { return multisampleCount; }
            set
            {
                AssertNotInitialized();
                if (value < 1) throw new ArgumentOutOfRangeException("value");

                multisampleCount = value;
            }
        }

        public int MultisampleQuality { get; protected set; }

        protected Texture2D(IDevice device)
            : base(device)
        {
            width = 1;
            height = 1;
            mipLevels = 1;
            format = SurfaceFormat.Color;
            multisampleCount = 1;
            MultisampleQuality = 0;
        }

        public void Initialize()
        {
            AssertNotInitialized();
            if (Usage == ResourceUsage.Immutable) throw new InvalidOperationException("Usage must be not immutable.");

            InitializeCore();

            initialized = true;
        }

        public void Initialize(Stream stream)
        {
            AssertNotInitialized();
            if (stream == null) throw new ArgumentNullException("stream");

            InitializeCore(stream);

            initialized = true;
        }

        public void Initialize(string path)
        {
            if (path == null) throw new ArgumentNullException("path");

            using (var stream = File.OpenRead(path))
            {
                Initialize(stream);
            }
        }

        public void Save(DeviceContext context, Stream stream, ImageFileFormat format = ImageFileFormat.Png)
        {
            AssertInitialized();
            if (context == null) throw new ArgumentNullException("context");
            if (stream == null) throw new ArgumentNullException("stream");

            SaveCore(context, stream, format);
        }

        // GetData メソッドは、デバッグ目的と位置付ける。
        // データ取得のために内部で Staging リソースをインスタンス化し、
        // データ取得後に破棄するため、GetData の頻繁な呼び出しは GC 負荷となり得る。

        public void GetData<T>(
            DeviceContext context, int level, Rectangle? rectangle, T[] data, int startIndex, int elementCount) where T : struct
        {
            AssertInitialized();
            if (context == null) throw new ArgumentNullException("context");
            if (data == null) throw new ArgumentNullException("data");
            if (startIndex < 0) throw new ArgumentOutOfRangeException("startIndex");
            if (data.Length < (startIndex + elementCount)) throw new ArgumentOutOfRangeException("elementCount");

            GetDataCore(context, level, rectangle, data, startIndex, elementCount);
        }

        public void GetData<T>(DeviceContext context, int level, T[] data) where T : struct
        {
            GetData(context, level, null, data, 0, data.Length);
        }

        public void GetData<T>(DeviceContext context, T[] data, int startIndex, int elementCount) where T : struct
        {
            GetData(context, 0, null, data, startIndex, elementCount);
        }

        public void GetData<T>(DeviceContext context, T[] data) where T : struct
        {
            GetData(context, 0, null, data, 0, data.Length);
        }

        public void SetData<T>(DeviceContext context, T[] data, int startIndex, int elementCount) where T : struct
        {
            SetData(context, 0, data, startIndex, elementCount);
        }

        public void SetData<T>(DeviceContext context, int level, T[] data, int startIndex, int elementCount) where T : struct
        {
            if (context == null) throw new ArgumentNullException("context");
            if (data == null) throw new ArgumentNullException("data");
            if (startIndex < 0) throw new ArgumentOutOfRangeException("startIndex");
            if (data.Length < (startIndex + elementCount)) throw new ArgumentOutOfRangeException("elementCount");

            if (Usage == ResourceUsage.Immutable)
                throw new InvalidOperationException("Data can not be set from CPU.");

            int levelWidth = Width >> level;

            // ブロック圧縮ならばブロック サイズで調整。
            // この場合、FormatHelper.SizeInBytes で測る値は、
            // 1 ブロック (4x4 テクセル) に対するバイト数である点に注意。
            if (FormatHelper.IsBlockCompression(Format))
            {
                levelWidth /= 4;
            }

            var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var dataPointer = gcHandle.AddrOfPinnedObject();
                var sizeOfT = Marshal.SizeOf(typeof(T));

                var sourcePointer = (IntPtr) (dataPointer + startIndex * sizeOfT);

                if (Usage == ResourceUsage.Default)
                {
                    // TODO
                    //
                    // Immutable と Dynamic 以外は UpdateSubresource で更新可能。
                    // Staging は内部利用にとどめるため Default でのみ UpdateSubresource で更新。
                    // それで良いのか？

                    int rowPitch = FormatHelper.SizeInBytes(Format) * levelWidth;
                    context.UpdateSubresource(this, level, null, sourcePointer, rowPitch, 0);
                }
                else
                {
                    var sizeInBytes = ((elementCount == 0) ? data.Length : elementCount) * sizeOfT;
                    
                    // ポインタの移動に用いるため、フォーマットから測れる要素サイズで算出しなければならない。
                    // SizeOf(typeof(T)) では、例えばバイト配列などを渡した場合に、
                    // そのサイズは元配列の要素の移動となり、リソース要素の移動にはならない。
                    var rowSpan = FormatHelper.SizeInBytes(Format) * levelWidth;

                    // TODO
                    //
                    // Dynamic だと D3D11MapMode.Write はエラーになる。
                    // 対応関係を MSDN から把握できないが、どうすべきか。
                    // ひとまず WriteDiscard とする。

                    var mappedResource = context.Map(this, level, DeviceContext.MapMode.WriteDiscard);
                    try
                    {
                        var rowSourcePointer = sourcePointer;
                        var destinationPointer = mappedResource.Pointer;

                        for (int i = 0; i < Height; i++)
                        {
                            GraphicsHelper.CopyMemory(destinationPointer, rowSourcePointer, rowSpan);
                            destinationPointer += mappedResource.RowPitch;
                            rowSourcePointer += rowSpan;
                        }
                    }
                    finally
                    {
                        context.Unmap(this, level);
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
            SetData(context, 0, data, 0, data.Length);
        }

        public void SetData<T>(DeviceContext context, int level, params T[] data) where T : struct
        {
            SetData(context, level, data, 0, data.Length);
        }

        protected abstract void InitializeCore();

        protected abstract void InitializeCore(Stream stream);

        protected abstract void SaveCore(DeviceContext context, Stream stream, ImageFileFormat format);

        protected abstract void GetDataCore<T>(
            DeviceContext context, int level, Rectangle? rectangle, T[] data, int startIndex, int elementCount) where T : struct;

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
