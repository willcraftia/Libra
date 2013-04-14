#region Using

using System;

using XA2AudioBuffer = SharpDX.XAudio2.AudioBuffer;
using XA2BufferFlags = SharpDX.XAudio2.BufferFlags;
using SDXMSpeakers = SharpDX.Multimedia.Speakers;
using SDXMWaveFormat = SharpDX.Multimedia.WaveFormat;
using SDXMWaveFormatAdpcm = SharpDX.Multimedia.WaveFormatAdpcm;
using SDXMWaveFormatExtensible = SharpDX.Multimedia.WaveFormatExtensible;

#endregion

namespace Libra.Audio.SharpDX
{
    public sealed class SdxSoundEffect : SoundEffect
    {
        public XA2AudioBuffer AudioBuffer { get; private set; }

        public XA2AudioBuffer LoopedAudioBuffer { get; private set; }

        public SDXMWaveFormat WaveFormat { get; private set; }

        public SdxSoundEffect(SdxSoundEffectManager manager)
            : base(manager)
        {
        }

        protected override void InitializeCore(WaveFormat format, IntPtr bufferPointer, int bufferSize)
        {
            AudioBuffer = new XA2AudioBuffer
            {
                AudioDataPointer = bufferPointer,
                AudioBytes = bufferSize,
                Flags = XA2BufferFlags.EndOfStream,
                PlayBegin = PlayBegin,
                PlayLength = PlayLength
            };

            int loopLength = PlayLength;
            if (PlayBegin == 0 && PlayLength == 0)
            {
                loopLength = bufferSize / (format.BitsPerSample / 8);
            }

            LoopedAudioBuffer = new XA2AudioBuffer
            {
                AudioDataPointer = bufferPointer,
                AudioBytes = bufferSize,
                Flags = XA2BufferFlags.EndOfStream,
                LoopBegin = PlayBegin,
                LoopLength = loopLength,
                LoopCount = XA2AudioBuffer.LoopInfinite
            };

            WaveFormat = new SDXMWaveFormat((int) format.SamplesPerSec, (int) format.BitsPerSample, (int) format.Channels);
        }

        protected override void InitializeCore(AdpcmWaveFormat format, IntPtr bufferPointer, int bufferSize)
        {
            AudioBuffer = new XA2AudioBuffer
            {
                AudioDataPointer = bufferPointer,
                AudioBytes = bufferSize,
                Flags = XA2BufferFlags.EndOfStream,
                PlayBegin = PlayBegin,
                PlayLength = PlayLength
            };

            int loopLength = PlayLength;
            if (PlayBegin == 0 && PlayLength == 0)
            {
                loopLength = bufferSize / (format.WaveFormat.BitsPerSample / 8);
            }

            LoopedAudioBuffer = new XA2AudioBuffer
            {
                AudioDataPointer = bufferPointer,
                AudioBytes = bufferSize,
                Flags = XA2BufferFlags.EndOfStream,
                LoopBegin = PlayBegin,
                LoopLength = loopLength,
                LoopCount = XA2AudioBuffer.LoopInfinite
            };

            var waveFormatAdpcm = new SDXMWaveFormatAdpcm(
                (int) format.WaveFormat.SamplesPerSec, (int) format.WaveFormat.Channels, format.WaveFormat.BlockAlign);

            if (waveFormatAdpcm.Coefficients1.Length != format.Coef.Length)
            {
                waveFormatAdpcm.Coefficients1 = new short[format.Coef.Length];
                waveFormatAdpcm.Coefficients2 = new short[format.Coef.Length];
            }
            for (int i = 0; i < format.Coef.Length; i++)
            {
                waveFormatAdpcm.Coefficients1[i] = format.Coef[i].Coef1;
                waveFormatAdpcm.Coefficients2[i] = format.Coef[i].Coef2;
            }

            WaveFormat = waveFormatAdpcm;
        }

        protected override void InitializeCore(WaveFormatExtensible format, IntPtr bufferPointer, int bufferSize)
        {
            AudioBuffer = new XA2AudioBuffer
            {
                AudioDataPointer = bufferPointer,
                AudioBytes = bufferSize,
                Flags = XA2BufferFlags.EndOfStream,
                PlayBegin = PlayBegin,
                PlayLength = PlayLength
            };

            int loopLength = PlayLength;
            if (PlayBegin == 0 && PlayLength == 0)
            {
                loopLength = bufferSize / (format.WaveFormat.BitsPerSample / 8);
            }

            LoopedAudioBuffer = new XA2AudioBuffer
            {
                AudioDataPointer = bufferPointer,
                AudioBytes = bufferSize,
                Flags = XA2BufferFlags.EndOfStream,
                LoopBegin = PlayBegin,
                LoopLength = loopLength,
                LoopCount = XA2AudioBuffer.LoopInfinite
            };

            var waveFormatExtensible = new SDXMWaveFormatExtensible(
                (int) format.WaveFormat.SamplesPerSec, (int) format.WaveFormat.BitsPerSample, (int) format.WaveFormat.Channels);

            // SharpDX の WaveFormatExtensible では
            // wValidBitsPerSample や wSamplesPerBlock を明示的に設定できない。
            waveFormatExtensible.ChannelMask = (SDXMSpeakers) format.ChannelMask;
            waveFormatExtensible.GuidSubFormat = format.SubFormat;

            WaveFormat = waveFormatExtensible;
        }
    }
}
