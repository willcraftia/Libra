#region Using

using System;
using System.Collections.Generic;
using Libra.Audio;
using Libra.Games;

#endregion

namespace Samples.Audio3D
{
    public sealed class AudioManager : GameComponent
    {
        #region ActiveSound

        class ActiveSound
        {
            public SoundEffectInstance Instance;

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

        public AudioManager(Game game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            SoundEffectManager.Default.DistanceScale = 2000;
            SoundEffectManager.Default.DopplerScale = 0.1f;

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

        public SoundEffectInstance Play3DSound(string soundName, bool isLooped, IAudioEmitter emitter)
        {
            var activeSound = new ActiveSound();

            activeSound.Instance = soundEffects[soundName].CreateInstance();
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
                    foreach (SoundEffect soundEffect in soundEffects.Values)
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
