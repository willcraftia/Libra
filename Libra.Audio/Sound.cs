#region Using

using System;

#endregion

namespace Libra.Audio
{
    public abstract class Sound
    {
        float volume;

        float pitch;

        float pan;

        bool paused;

        public SoundManager SoundManager { get; private set; }

        public SoundState State
        {
            get
            {
                if (GetBuffersQueued() == 0)
                    return SoundState.Stopped;

                if (paused)
                    return SoundState.Paused;

                return SoundState.Playing;
            }
        }

        public float Volume
        {
            get { return volume; }
            set
            {
                if (value < 0.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                if (volume == value) return;

                volume = value;

                OnVolumeChanged();
            }
        }

        public float Pitch
        {
            get { return pitch; }
            set
            {
                if (value < -1.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                if (pitch == value) return;

                pitch = value;

                OnPitchChanged();
            }
        }

        public float Pan
        {
            get { return pan; }
            set
            {
                if (value < -1.0f || 1.0f < value) throw new ArgumentOutOfRangeException("value");

                if (pitch == value) return;

                pan = value;

                OnPanChanged();
            }
        }

        public bool IsLooped { get; set; }

        protected Sound(SoundManager soundManager)
        {
            if (soundManager == null) throw new ArgumentNullException("soundManager");

            SoundManager = soundManager;

            volume = 1.0f;
            pitch = 0.0f;
            pan = 0.0f;
        }

        public void Apply3D(AudioListener listener, AudioEmitter emitter)
        {
            if (listener == null) throw new ArgumentNullException("listener");
            if (emitter == null) throw new ArgumentNullException("emitter");

            Apply3DCore(listener, emitter);
        }

        public void Apply3D(AudioListener[] listeners, AudioEmitter emitter)
        {
            foreach (var listener in listeners)
                Apply3D(listener, emitter);
        }

        public void Play()
        {
            if (State == SoundState.Playing) return;

            if (0 < GetBuffersQueued())
            {
                Stop();
            }

            PlayCore();

            paused = true;
        }

        public void Pause()
        {
            if (paused) return;

            PauseCore();

            paused = true;
        }

        public void Resume()
        {
            ResumeCore();

            paused = false;
        }

        public void Stop(bool immediate = true)
        {
            StopCore(immediate);

            paused = false;
        }

        protected abstract int GetBuffersQueued();

        protected abstract void Apply3DCore(AudioListener listener, AudioEmitter emitter);

        protected abstract void PlayCore();

        protected abstract void PauseCore();

        protected abstract void ResumeCore();

        protected abstract void StopCore(bool immediate);

        protected abstract void OnVolumeChanged();

        protected abstract void OnPitchChanged();

        protected abstract void OnPanChanged();

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~Sound()
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

            if (disposing)
            {
                SoundManager.ReleaseSound(this);
            }

            DisposeOverride(disposing);

            IsDisposed = true;
        }

        #endregion
    }
}
