#region Using

using System;

#endregion

namespace Libra.Audio
{
    public abstract class SoundEffectInstance : IDisposable
    {
        float volume;

        float pitch;

        float pan;

        public SoundEffectManager Manager { get; private set; }

        public SoundState State { get; private set; }

        protected SoundEffect SoundEffect { get; private set; }

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

        protected SoundEffectInstance(SoundEffectManager manager, SoundEffect soundEffect = null)
        {
            if (manager == null) throw new ArgumentNullException("manager");

            Manager = manager;

            volume = 1.0f;
            pitch = 0.0f;
            pan = 0.0f;

            State = SoundState.Stopped;
        }

        internal void Initialize(SoundEffect soundEffect)
        {
            SoundEffect = soundEffect;

            InitializeCore();
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

            if (State != SoundState.Stopped)
            {
                Stop();
            }

            PlayCore();

            State = SoundState.Playing;
        }

        public void Pause()
        {
            if (State == SoundState.Paused) return;

            PauseCore();

            State = SoundState.Paused;
        }

        public void Resume()
        {
            if (State != SoundState.Paused) return;

            ResumeCore();

            State = SoundState.Playing;
        }

        public void Stop(bool immediate = true)
        {
            if (State == SoundState.Stopped) return;

            StopCore(immediate);

            State = SoundState.Stopped;
        }

        protected abstract void InitializeCore();

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

        ~SoundEffectInstance()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeOverride(bool disposing) { }

        internal void DisposeByManager()
        {
            if (IsDisposed) return;

            // マネージャからの破棄の呼び出しならば参照解放をスキップ。
            
            DisposeOverride(true);

            IsDisposed = true;
        }

        void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            // Dispose による明示的な破棄ならばマネージャからの参照を解放。
            if (disposing)
            {
                Manager.ReleaseSoundEffectInstance(this);
            }

            DisposeOverride(disposing);

            IsDisposed = true;
        }

        #endregion
    }
}
