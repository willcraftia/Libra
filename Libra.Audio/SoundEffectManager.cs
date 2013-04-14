#region Using

using System;
using System.Collections.Generic;
using System.Configuration;

#endregion

namespace Libra.Audio
{
    public abstract class SoundEffectManager : IDisposable
    {
        public const string AppSettingKey = "Libra.Audio.SoundEffectManager";

        const string DefaultImplementation = "Libra.Audio.SharpDX.SdxSoundEffectManager, Libra.Audio.SharpDX, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        float masterVolume;

        float distanceScale;

        float dopplerScale;

        List<SoundEffectInstance> instancesToDispose;

        bool skipReleaseInstance;

        public static SoundEffectManager Default { get; private set; }

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

        static SoundEffectManager()
        {
            Default = CreateSoundEffectManager();
        }

        protected SoundEffectManager()
        {
            masterVolume = 1.0f;
            distanceScale = 1.0f;
            dopplerScale = 1.0f;

            instancesToDispose = new List<SoundEffectInstance>();
        }

        public static SoundEffectManager CreateSoundEffectManager()
        {
            // app.config 定義を参照。
            var implementation = ConfigurationManager.AppSettings[AppSettingKey];

            // app.config で未定義ならば SharpDX 実装をデフォルト指定。
            if (implementation == null)
                implementation = DefaultImplementation;

            var type = Type.GetType(implementation);
            return Activator.CreateInstance(type) as SoundEffectManager;
        }

        public abstract SoundEffect CreateSoundEffect();

        internal SoundEffectInstance CreateSoundEffectInstance()
        {
            var instance = CreateSoundEffectInstanceCore();

            instancesToDispose.Add(instance);

            return instance;
        }

        internal void ReleaseSoundEffectInstance(SoundEffectInstance instance)
        {
            if (!skipReleaseInstance)
                instancesToDispose.Remove(instance);
        }

        protected abstract SoundEffectInstance CreateSoundEffectInstanceCore();

        protected abstract void OnMasterVolumeChanged();

        #region IDisposable

        public bool IsDisposed { get; private set; }

        ~SoundEffectManager()
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

            skipReleaseInstance = true;

            if (disposing)
            {
                foreach (var instance in instancesToDispose)
                {
                    instance.Dispose();
                }
            }

            DisposeOverride(disposing);

            IsDisposed = true;
        }

        #endregion
    }
}
