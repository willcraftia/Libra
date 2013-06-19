#region Using

using System;
using System.Collections.Generic;
using Libra;
using Libra.Audio;
using Libra.Games;

#endregion

namespace Samples.Audio3D
{
    // サンプル オリジナルのクラス名 AudioManager は、
    // Libra.Games.AudioManager と同名であることから混乱を招くため、
    // クラス名を AudioComponent としている。

    public sealed class AudioComponent : GameComponent
    {
        #region ActiveSound

        class ActiveSound
        {
            public StaticSound Instance;

            public IAudioEmitter Emitter;
        }

        #endregion

        static string[] soundNames =
        {
            "CatSound0",
            "CatSound1",
            "CatSound2",
            "DogSound",
        };

        AudioListener listener = new AudioListener();

        AudioEmitter emitter = new AudioEmitter();

        Dictionary<string, SoundEffect> soundEffects = new Dictionary<string, SoundEffect>();

        List<ActiveSound> activeSounds = new List<ActiveSound>();

        public AudioListener Listener
        {
            get { return listener; }
        }

        public AudioComponent(Game game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            (Game as MainGame).Audio.SoundManager.DistanceScale = 2000;
            (Game as MainGame).Audio.SoundManager.DopplerScale = 0.1f;

            foreach (string soundName in soundNames)
            {
                soundEffects.Add(soundName, (Game as MainGame).Content.Load<SoundEffect>(soundName));
            }

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            int index = 0;

            while (index < activeSounds.Count)
            {
                var activeSound = activeSounds[index];

                if (activeSound.Instance.State == SoundState.Stopped)
                {
                    activeSound.Instance.Dispose();

                    activeSounds.RemoveAt(index);
                }
                else
                {
                    Apply3D(activeSound);

                    index++;
                }
            }

            base.Update(gameTime);
        }

        public StaticSound Play3DSound(string soundName, bool isLooped, IAudioEmitter emitter)
        {
            var activeSound = new ActiveSound();

            activeSound.Instance = soundEffects[soundName].CreateSound();
            activeSound.Instance.IsLooped = isLooped;

            activeSound.Emitter = emitter;

            Apply3D(activeSound);

            activeSound.Instance.Play();

            activeSounds.Add(activeSound);

            return activeSound.Instance;
        }

        void Apply3D(ActiveSound activeSound)
        {
            emitter.Position = activeSound.Emitter.Position;
            emitter.Forward = activeSound.Emitter.Forward;
            emitter.Up = activeSound.Emitter.Up;
            emitter.Velocity = activeSound.Emitter.Velocity;

            activeSound.Instance.Apply3D(listener, emitter);
        }

        protected override void DisposeOverride(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    foreach (var soundEffect in soundEffects.Values)
                    {
                        soundEffect.Dispose();
                    }

                    soundEffects.Clear();
                }
            }
            finally
            {
                base.DisposeOverride(disposing);
            }
        }
    }
}
