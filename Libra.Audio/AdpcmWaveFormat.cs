#region Using

using System;

#endregion

namespace Libra.Audio
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// ADPCMWAVEFORMAT
    /// </remarks>
    public struct AdpcmWaveFormat
    {
        public WaveFormat WaveFormat;

        public ushort SamplesPerBlock;

        public ushort NumCoef;

        public AdpcmCoefSet[] Coef;
    }
}
