#region Using

using System;
using System.IO;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Graphics
{
    public abstract class Texture2D : Resource
    {
        internal bool initialized;

        int width = 1;

        int height = 1;

        int mipLevels = 1;

        int arraySize = 1;

        SurfaceFormat format = SurfaceFormat.Color;

        int preferredMultisampleCount = 1;

        ShaderResourceView shaderResourceView;

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

        public int ArraySize
        {
            get { return arraySize; }
            set
            {
                AssertNotInitialized();
                if (value < 1 || D3D11Constants.ReqTexture2dArrayAxisDimension < value)
                    throw new ArgumentOutOfRangeException("value");

                arraySize = value;
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

        public int PreferredMultisampleCount
        {
            get { return preferredMultisampleCount; }
            set
            {
                AssertNotInitialized();
                if (!MathHelper.IsPowerOf2(value))
                    throw new ArgumentException("The specified value is not a power of 2.", "value");

                preferredMultisampleCount = value;
            }
        }

        public int MultisampleCount { get; protected set; }

        public int MultisampleQuality { get; protected set; }

        public Rectangle Bounds
        {
            get { return new Rectangle(0, 0, width, height); }
        }

        protected Texture2D(Device device)
            : base(device)
        {
            MultisampleCount = 1;
            MultisampleQuality = 0;
        }

        public void Initialize()
        {
            AssertNotInitialized();
            if (Usage == ResourceUsage.Immutable) throw new InvalidOperationException("Usage must be not immutable.");

            // マルチサンプリングを有効にする場合はミップマップ レベルが 1 でなければならない。
            if (1 < preferredMultisampleCount && mipLevels != 1)
                throw new InvalidOperationException("MipLevels must be 1 for a multisampled texture.");

            for (int i = preferredMultisampleCount; 1 < i; i /= 2)
            {
                var multisampleQualityLevels = Device.CheckMultisampleQualityLevels(format, i);
                if (0 < multisampleQualityLevels)
                {
                    MultisampleCount = i;
                    MultisampleQuality = multisampleQualityLevels - 1;
                    break;
                }
            }

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

        /// <summary>
        /// 暗黙的に GetShaderResourceView() を呼び出して ShaderResourceView 型とします。
        /// </summary>
        /// <param name="texture">Texture2D。</param>
        /// <returns>Texture2D 内部で管理する ShaderResourceView。</returns>
        public static implicit operator ShaderResourceView(Texture2D texture)
        {
            if (texture == null) return null;

            return texture.GetShaderResourceView();
        }

        public ShaderResourceView GetShaderResourceView()
        {
            if (shaderResourceView == null)
            {
                shaderResourceView = Device.CreateShaderResourceView();
                shaderResourceView.Initialize(this);
            }
            return shaderResourceView;
        }

        public void Save(DeviceContext context, Stream stream, ImageFileFormat format = ImageFileFormat.Png)
        {
            AssertInitialized();

            context.Save(this, stream, format);
        }

        // GetData メソッドは、データ取得のために内部で Staging リソースをインスタンス化し、
        // データ取得後に破棄するため、頻繁な呼び出しは GC 負荷となり得る。

        public void GetData<T>(DeviceContext context, T[] data) where T : struct
        {
            GetData(context, 0, 0, null, data, 0, data.Length);
        }

        public void GetData<T>(DeviceContext context, int arrayIndex, T[] data) where T : struct
        {
            GetData(context, arrayIndex, 0, null, data, 0, data.Length);
        }

        public void GetData<T>(DeviceContext context, T[] data, int startIndex, int elementCount) where T : struct
        {
            GetData(context, 0, 0, null, data, startIndex, elementCount);
        }

        public void GetData<T>(
            DeviceContext context,
            int arrayIndex,
            T[] data,
            int startIndex,
            int elementCount) where T : struct
        {
            GetData(context, arrayIndex, 0, null, data, startIndex, elementCount);
        }

        public void GetData<T>(
            DeviceContext context,
            int arrayIndex,
            int mipLevel,
            Rectangle? rectangle,
            T[] data,
            int startIndex,
            int elementCount) where T : struct
        {
            AssertInitialized();

            context.GetData(this, arrayIndex, mipLevel, rectangle, data, startIndex, elementCount);
        }

        public void SetData<T>(DeviceContext context, params T[] data) where T : struct
        {
            SetData(context, 0, 0, data, 0, data.Length);
        }

        public void SetData<T>(DeviceContext context, int arrayIndex, params T[] data) where T : struct
        {
            SetData(context, arrayIndex, 0, data, 0, data.Length);
        }

        public void SetData<T>(DeviceContext context, T[] data, int startIndex, int elementCount) where T : struct
        {
            SetData(context, 0, 0, data, startIndex, elementCount);
        }

        public void SetData<T>(DeviceContext context, int arrayIndex, T[] data, int startIndex, int elementCount) where T : struct
        {
            SetData(context, arrayIndex, 0, data, startIndex, elementCount);
        }

        public void SetData<T>(DeviceContext context, int arrayIndex, int mipLevel, T[] data, int startIndex, int elementCount) where T : struct
        {
            AssertInitialized();

            context.SetData(this, arrayIndex, mipLevel, data, startIndex, elementCount);
        }

        public void SetData<T>(DeviceContext context, int arrayIndex, int mipLevel, Rectangle? rectangle, T[] data, int startIndex, int elementCount) where T : struct
        {
            AssertInitialized();

            context.SetData(this, arrayIndex, mipLevel, rectangle, data, startIndex, elementCount);
        }

        protected abstract void InitializeCore();

        protected abstract void InitializeCore(Stream stream);

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                if (shaderResourceView != null)
                    shaderResourceView.Dispose();
            }

            base.DisposeOverride(disposing);
        }

        internal void AssertNotInitialized()
        {
            if (initialized) throw new InvalidOperationException("Already initialized.");
        }

        internal void AssertInitialized()
        {
            if (!initialized) throw new InvalidOperationException("Not initialized.");
        }
    }
}
