#region Using

using System;

#endregion

namespace Libra.Audio
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// WAVEFORMATEX
    /// </remarks>
    public struct WaveFormat
    {
        public WaveFormatTag FormatTag;

        public ushort Channels;

        public uint SamplesPerSec;

        public uint AvgBytesPerSec;

        public ushort BlockAlign;

        public ushort BitsPerSample;

        public ushort ExtraSize;
    }
}
