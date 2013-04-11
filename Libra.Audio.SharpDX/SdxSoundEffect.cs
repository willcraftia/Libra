#region Using

using System;

using XA2AudioBuffer = SharpDX.XAudio2.AudioBuffer;
using XA2BufferFlags = SharpDX.XAudio2.BufferFlags;
using SDXMWaveFormat = SharpDX.Multimedia.WaveFormat;

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

        protected override void InitializeCore(IntPtr bufferPointer, int bufferSize, int sampleRate, AudioChannels channels)
        {
            AudioBuffer = new XA2AudioBuffer
            {
                AudioDataPointer = bufferPointer,
                AudioBytes = bufferSize,
                Flags = XA2BufferFlags.EndOfStream,
                PlayBegin = 0,
                PlayLength = bufferSize
            };

            LoopedAudioBuffer = new XA2AudioBuffer
            {
                AudioDataPointer = bufferPointer,
                AudioBytes = bufferSize,
                Flags = XA2BufferFlags.EndOfStream,
                LoopBegin = LoopStart,
                LoopLength = (LoopLength == 0) ? bufferSize : LoopLength,
                LoopCount = XA2AudioBuffer.LoopInfinite
            };

            WaveFormat = new SDXMWaveFormat(sampleRate, (int) channels);
        }

        protected override SoundEffectInstance CreateInstanceCore()
        {
            return new SdxSoundEffectInstance(this);
        }
    }
}
