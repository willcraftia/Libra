#region Using

using System;
using System.Collections.Generic;
using System.Configuration;

#endregion

namespace Libra.Audio
{
    public abstract class SoundManager : IDisposable
    {
        public const string AppSettingKey = "Libra.Audio.SoundManager";

        const string DefaultImplementation = "Libra.Audio.SharpDX.SdxSoundManager, Libra.Audio.SharpDX, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        float masterVolume;

        float distanceScale;

        float dopplerScale;

        List<Sound> soundsToDispose;

        bool skipReleaseSound;

        public float MasterVolume
        {
            get { return masterVolume; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value");

                if (masterVolume == value) return;

                masterVolume = value;

                OnMasterVolumeChanged();
            }
        }

        public float DistanceScale
        {
            get { return distanceScale; }
            set
            {
                if (value <= 0.0f) throw new ArgumentOutOfRangeException("value");

                distanceScale = value;
            }
        }

        public float DopplerScale
        {
            get { return dopplerScale; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                dopplerScale = value;
            }
        }

        protected SoundManager()
        {
            masterVolume = 1.0f;
            distanceScale = 1.0f;
            dopplerScale = 1.0f;

            soundsToDispose = new List<Sound>();
        }

        public static SoundManager CreateSoundManager()
        {
            // app.config 定義を参照。
            var implementation = ConfigurationManager.AppSettings[AppSettingKey];

            // app.config で未定義ならば SharpDX 実装をデフォルト指定。
            if (implementation == null)
                implementation = DefaultImplementation;

            var type = Type.GetType(implementation);
            return Activator.CreateInstance(type) as SoundManager;
        }

        public abstract AudioBuffer CreateAudioBuffer();

        public StaticSound CreateStaticSound()
        {
            var instance = CreateStaticSoundCore();

            soundsToDispose.Add(instance);

            return instance;
        }

        internal void ReleaseSound(Sound sound)
        {
            if (!skipReleaseSound)
                soundsToDispose.Remove(sound);
        }

        protected abstract StaticSound CreateStaticSoundCore();

        protected abstract void OnMasterVolumeChanged();

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~SoundManager()
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

            skipReleaseSound = true;

            if (disposing)
            {
                foreach (var sound in soundsToDispose)
                {
                    sound.Dispose();
                }
            }

            DisposeOverride(disposing);

            IsDisposed = true;
        }

        #endregion
    }
}
