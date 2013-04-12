#region Using

using System;

#endregion

namespace Libra.Audio
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// WAVEFORMATEXTENSIBLE
    /// </remarks>
    public struct WaveFormatExtensible
    {
        public WaveFormat WaveFormat;

        // union: wValidBitsPerSample/wSamplesPerBlock/wReserved
        public ushort Samples;

        public uint ChannelMask;

        public Guid SubFormat;
    }
}
