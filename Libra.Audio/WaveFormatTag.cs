#region Using

using System;

#endregion

namespace Libra.Audio
{
    public enum WaveFormatTag : ushort
    {
        Unknown     = 0,
        Pcm         = 0x0001,
        Adpcm       = 0x0002,
        IeeeFloat   = 0x0003,
        Wmaudio2    = 0x0161,
        Wmaudio3    = 0x0162,
        // 対応しない。
        // SharpDX でも対応する列挙値が無い。
        //Xma2        = 0x166,
        Extensible = 0xfffe,
    }
}
