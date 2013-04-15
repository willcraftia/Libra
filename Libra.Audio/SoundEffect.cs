#region Using

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#endregion

namespace Libra.Audio
{
    public sealed class SoundEffect : IDisposable
    {
        Queue<StaticSound> activeSounds;

        Queue<StaticSound> freeSounds;

        public SoundManager SoundManager { get; private set; }

        public AudioBuffer AudioBuffer { get; private set; }

        public SoundEffect(SoundManager soundManager, AudioBuffer audioBuffer)
        {
            if (soundManager == null) throw new ArgumentNullException("soundManager");
            if (audioBuffer == null) throw new ArgumentNullException("audioBuffer");

            SoundManager = soundManager;
            AudioBuffer = audioBuffer;

            activeSounds = new Queue<StaticSound>();
            freeSounds = new Queue<StaticSound>();
        }

        // XNB からの SoundEffect 生成からの利用のための互換メソッド。
        // 直接 AudioBuffer を作るならば、SoundEffect を経由する必要はない。
        public StaticSound CreateSound()
        {
            var sound = SoundManager.CreateStaticSound();
            sound.Initialize(AudioBuffer);
            return sound;
        }

        public void Play(float volume = 1.0f, float pitch = 0.0f, float pan = 0.0f)
        {
            if (volume < 0.0f || 1.0f < volume) throw new ArgumentOutOfRangeException("volume");
            if (pitch < -1.0f || 1.0f < pitch) throw new ArgumentOutOfRangeException("pitch");
            if (pan < -1.0f || 1.0f < pan) throw new ArgumentOutOfRangeException("pan");

            int activeInstanceCount = activeSounds.Count;
            for (int i = 0; i < activeInstanceCount; i++)
            {
                var activeInstance = activeSounds.Dequeue();
                if (activeInstance.State == SoundState.Stopped)
                {
                    // 停止しているならばプールへ戻す。
                    freeSounds.Enqueue(activeInstance);
                }
                else
                {
                    // 停止していないならばアクティブ。
                    activeSounds.Enqueue(activeInstance);
                }
            }

            StaticSound sound;
            if (0 < freeSounds.Count)
            {
                sound = freeSounds.Dequeue();
            }
            else
            {
                sound = CreateSound();
            }

            activeSounds.Enqueue(sound);

            sound.Volume = volume;
            sound.Pitch = pitch;
            sound.Pan = pan;

            sound.Play();
        }

        #region IDisposable

        bool disposed;

        ~SoundEffect()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                while (0 < activeSounds.Count)
                {
                    var sound = activeSounds.Dequeue();

                    if (sound.State != SoundState.Stopped)
                        sound.Stop();

                    freeSounds.Enqueue(sound);
                }

                while (0 < freeSounds.Count)
                {
                    var sound = freeSounds.Dequeue();
                    sound.Dispose();
                }
            }

            disposed = true;
        }

        #endregion
    }
}
