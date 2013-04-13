#region Using

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Audio
{
    public abstract class SoundEffect : IDisposable
    {
        bool initialized;

        int loopStart;

        int loopLength;

        GCHandle gcHandle;

        bool pinned;

        TimeSpan duration;

        Queue<SoundEffectInstance> activeInstances;

        Queue<SoundEffectInstance> freeInstances;

        public SoundEffectManager Manager { get; private set; }

        public int LoopStart
        {
            get { return loopStart; }
            set
            {
                AssertNotInitialized();
                if (value < 0) throw new ArgumentOutOfRangeException("value");

                loopStart = value;
            }
        }

        public int LoopLength
        {
            get { return loopLength; }
            set
            {
                AssertNotInitialized();
                if (value < 0) throw new ArgumentOutOfRangeException("value");

                loopLength = value;
            }
        }

        public TimeSpan Duration
        {
            get { return duration; }
        }

        protected SoundEffect(SoundEffectManager manager)
        {
            if (manager == null) throw new ArgumentNullException("manager");

            Manager = manager;
        }

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

        public void Play(float volume = 1.0f, float pitch = 0.0f, float pan = 0.0f)
        {
            AssertInitialized();
            if (volume < 0.0f || 1.0f < volume) throw new ArgumentOutOfRangeException("volume");
            if (pitch < -1.0f || 1.0f < pitch) throw new ArgumentOutOfRangeException("pitch");
            if (pan < -1.0f || 1.0f < pan) throw new ArgumentOutOfRangeException("pan");

            if (activeInstances == null)
            {
                activeInstances = new Queue<SoundEffectInstance>();
                freeInstances = new Queue<SoundEffectInstance>();
            }
            else
            {
                int activeInstanceCount = activeInstances.Count;
                for (int i = 0; i < activeInstanceCount; i++)
                {
                    var activeInstance = activeInstances.Dequeue();
                    if (activeInstance.State == SoundState.Stopped)
                    {
                        // 停止しているならばプールへ戻す。
                        freeInstances.Enqueue(activeInstance);
                    }
                    else
                    {
                        // 停止していないならばアクティブ。
                        activeInstances.Enqueue(activeInstance);
                    }
                }
            }

            SoundEffectInstance instance;
            if (0 < freeInstances.Count)
            {
                instance = freeInstances.Dequeue();
            }
            else
            {
                instance = CreateInstance();
            }

            activeInstances.Enqueue(instance);

            instance.Volume = volume;
            instance.Pitch = pitch;
            instance.Pan = pan;

            instance.Play();
        }

        public SoundEffectInstance CreateInstance()
        {
            AssertInitialized();

            var instance = Manager.CreateSoundEffectInstance();

            instance.Initialize(this);

            return instance;
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

        ~SoundEffect()
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
