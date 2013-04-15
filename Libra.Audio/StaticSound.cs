#region Using

using System;

#endregion

namespace Libra.Audio
{
    public abstract class StaticSound : Sound
    {
        public AudioBuffer AudioBuffer { get; private set; }

        protected StaticSound(SoundManager manager)
            : base(manager)
        {
        }

        public void Initialize(AudioBuffer audioBuffer)
        {
            if (audioBuffer == null) throw new ArgumentNullException("audioBuffer");

            AudioBuffer = audioBuffer;

            InitializeCore();
        }

        protected abstract void InitializeCore();
    }
}
