#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public abstract class SwapChain : IDisposable
    {
        public event EventHandler BackBuffersResizing;

        public event EventHandler BackBuffersResized;

        public IDevice Device { get; private set; }

        public abstract int BackBufferWidth { get; }

        public abstract int BackBufferHeight { get; }

        public abstract SurfaceFormat BackBufferFormat { get; }

        public abstract int BackBufferMultiSampleCount { get; }

        public abstract int BackBufferMultiSampleQuality { get; }

        public abstract bool Windowed { get; }

        public abstract bool AllowModeSwitch { get; }

        public abstract DepthFormat DepthStencilFormat { get; }

        public abstract int SyncInterval { get; }

        protected SwapChain(IDevice device)
        {
            if (device == null) throw new ArgumentNullException("device");

            Device = device;
        }

        public abstract void Present();

        public void ResizeBuffers(int width, int height)
        {
            ResizeBuffers(width, height, 1, BackBufferFormat);
        }

        public void ResizeBuffers(int width, int height, int bufferCount, SurfaceFormat format)
        {
            if (width < 0) throw new ArgumentOutOfRangeException("width");
            if (height < 0) throw new ArgumentOutOfRangeException("height");
            if (bufferCount < 0) throw new ArgumentOutOfRangeException("bufferCount");

            // width = 0 や height = 0 の場合、
            // 対象ウィンドウのクライアント領域のサイズが用いられる。
            // bufferCount = 0 の場合、既存のバッファ数が保持される。

            // ResizeBuffers の前には、スワップ チェーンに関連付けられた
            // 全てのリソースを解放しなければならない。
            // このため、Device は ResizingBuffers と ResizedBuffers の発生を受け、
            // その内部で管理するバックバッファのためのビュー等を破棄するようにする。

            if (BackBuffersResizing != null)
                BackBuffersResizing(this, EventArgs.Empty);

            ResizeBuffersCore(width, height, bufferCount, format);

            if (BackBuffersResized != null)
                BackBuffersResized(this, EventArgs.Empty);
        }

        public void ResizeTarget()
        {
            ResizeTarget(BackBufferWidth, BackBufferHeight);
        }

        public void ResizeTarget(int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException("width");
            if (height < 0) throw new ArgumentOutOfRangeException("height");

            // width = 0 や height = 0 の場合、
            // 対象ウィンドウのクライアント領域のサイズが用いられる。

            ResizeTargetCore(width, height);
        }

        protected abstract void ResizeBuffersCore(int width, int height, int bufferCount, SurfaceFormat format);

        protected abstract void ResizeTargetCore(int width, int height);

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~SwapChain()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeOverride(bool disposing) { }

        void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            DisposeOverride(disposing);

            IsDisposed = true;
        }

        #endregion
    }
}
