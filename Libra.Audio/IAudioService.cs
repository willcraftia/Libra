#region Using

using System;

#endregion

namespace Libra.Audio
{
    public interface IAudioService
    {
        SoundManager SoundManager { get; }
    }
}
