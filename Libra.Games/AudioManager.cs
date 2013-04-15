#region Using

using System;
using Libra.Audio;

#endregion

namespace Libra.Games
{
    public sealed class AudioManager : IAudioService, IDisposable
    {
        Game game;

        public SoundManager SoundManager { get; private set; }

        public AudioManager(Game game)
        {
            if (game == null) throw new ArgumentNullException("game");

            this.game = game;

            SoundManager = SoundManager.CreateSoundManager();

            game.Services.AddService<IAudioService>(this);
        }

        #region IDisposable

        bool disposed;

        ~AudioManager()
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
                SoundManager.Dispose();
            }

            disposed = true;
        }

        #endregion
    }
}
