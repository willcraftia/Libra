#region Using

using System;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Audio
{
    public abstract class AudioBuffer : IDisposable
    {
        bool initialized;

        int playBegin;

        int playLength;

        GCHandle gcHandle;

        bool pinned;

        TimeSpan duration;

        public int PlayBegin
        {
            get { return playBegin; }
            set
            {
                AssertNotInitialized();
                if (value < 0) throw new ArgumentOutOfRangeException("value");

                playBegin = value;
            }
        }

        // PlayLength = 0 はバッファ全体の再生を示す。
        public int PlayLength
        {
            get { return playLength; }
            set
            {
                AssertNotInitialized();
                if (value < 0) throw new ArgumentOutOfRangeException("value");

                playLength = value;
            }
        }

        public TimeSpan Duration
        {
            get { return duration; }
        }

        protected AudioBuffer() { }

        public void Initialize(WaveFormat format, byte[] buffer, int offset, int count)
        {
            AssertNotInitialized();
            if (buffer == null) throw new ArgumentNullException("buffer");

            duration = TimeSpan.FromSeconds((float) count / (float) format.AvgBytesPerSec);

            gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            pinned = true;

            var bufferPointer = gcHandle.AddrOfPinnedObject() + offset;

            InitializeCore(format, bufferPointer, count);

            initialized = true;
        }

        public void Initialize(AdpcmWaveFormat format, byte[] buffer, int offset, int count)
        {
            AssertNotInitialized();
            if (buffer == null) throw new ArgumentNullException("buffer");

            duration = TimeSpan.FromSeconds((float) count / (float) format.WaveFormat.AvgBytesPerSec);

            gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            pinned = true;

            var bufferPointer = gcHandle.AddrOfPinnedObject() + offset;

            InitializeCore(format, bufferPointer, count);

            initialized = true;
        }

        public void Initialize(WaveFormatExtensible format, byte[] buffer, int offset, int count)
        {
            AssertNotInitialized();
            if (buffer == null) throw new ArgumentNullException("buffer");

            duration = TimeSpan.FromSeconds((float) count / (float) format.WaveFormat.AvgBytesPerSec);

            gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            pinned = true;

            var bufferPointer = gcHandle.AddrOfPinnedObject() + offset;

            InitializeCore(format, bufferPointer, count);

            initialized = true;
        }

        protected abstract void InitializeCore(WaveFormat format, IntPtr bufferPointer, int bufferSize);

        protected abstract void InitializeCore(AdpcmWaveFormat format, IntPtr bufferPointer, int bufferSize);

        protected abstract void InitializeCore(WaveFormatExtensible format, IntPtr bufferPointer, int bufferSize);

        void AssertNotInitialized()
        {
            if (initialized) throw new InvalidOperationException("Already initialized.");
        }

        void AssertInitialized()
        {
            if (!initialized) throw new InvalidOperationException("Not initialized.");
        }

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~AudioBuffer()
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

            if (pinned)
            {
                gcHandle.Free();
                pinned = false;
            }

            DisposeOverride(disposing);

            IsDisposed = true;
        }

        #endregion
    }
}
