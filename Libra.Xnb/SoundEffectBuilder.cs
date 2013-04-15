#region Using

using System;
using System.Runtime.InteropServices;
using Felis;
using Libra.Audio;

#endregion

namespace Libra.Xnb
{
    public sealed class SoundEffectBuilder : SoundEffectBuilderBase<SoundEffect>
    {
        #region FormatType

        enum FormatType
        {
            WaveFormat,
            WaveFormatExtensible,
            AdpcmWaveFormat
        }

        #endregion

        AudioBuffer audioBuffer;

        FormatType formatType;

        WaveFormat waveFormat;

        WaveFormatExtensible waveFormatExtensible;

        AdpcmWaveFormat adpcmWaveFormat;

        byte[] data;

        protected override void SetFormatSize(uint value) { }

        protected override void SetFormat(byte[] values)
        {
            // http://msdn.microsoft.com/en-us/library/microsoft.directx_sdk.xaudio2.waveformatex(v=vs.85).aspx

            var formatTag = (WaveFormatTag) BitConverter.ToUInt16(values, 0);

            switch (formatTag)
            {
                case WaveFormatTag.Pcm:
                case WaveFormatTag.IeeeFloat:
                    CreateWaveFormat(values);
                    break;
                case WaveFormatTag.Extensible:
                    CreateWaveFormatExtensible(values);
                    break;
                case WaveFormatTag.Adpcm:
                    CreateAdpcmWaveFormat(values);
                    break;
                case WaveFormatTag.Wmaudio2:
                case WaveFormatTag.Wmaudio3:
                    var channels = BitConverter.ToUInt16(values, 2);
                    if (2 < channels)
                    {
                        CreateWaveFormatExtensible(values);
                    }
                    else
                    {
                        CreateWaveFormat(values);
                    }
                    break;
                default:
                    throw new NotSupportedException("Unsupported wave format: " + formatTag);
            }
        }

        void CreateWaveFormat(byte[] values)
        {
            var gcHandle = GCHandle.Alloc(values, GCHandleType.Pinned);
            try
            {
                waveFormat = (WaveFormat) Marshal.PtrToStructure(
                    gcHandle.AddrOfPinnedObject(), typeof(WaveFormat));
            }
            finally
            {
                gcHandle.Free();
            }

            formatType = FormatType.WaveFormat;
        }

        void CreateWaveFormatExtensible(byte[] values)
        {
            var gcHandle = GCHandle.Alloc(values, GCHandleType.Pinned);
            try
            {
                waveFormatExtensible = (WaveFormatExtensible) Marshal.PtrToStructure(
                    gcHandle.AddrOfPinnedObject(), typeof(WaveFormatExtensible));
            }
            finally
            {
                gcHandle.Free();
            }

            formatType = FormatType.WaveFormatExtensible;
        }

        void CreateAdpcmWaveFormat(byte[] values)
        {
            var gcHandle = GCHandle.Alloc(values, GCHandleType.Pinned);
            try
            {
                // TODO
                // 配列を含む構造体であるため、エラーになるんじゃないかと・・・。
                // もしエラーになるならば、手動でマーシャリングする。
                adpcmWaveFormat = (AdpcmWaveFormat) Marshal.PtrToStructure(
                    gcHandle.AddrOfPinnedObject(), typeof(AdpcmWaveFormat));
            }
            finally
            {
                gcHandle.Free();
            }

            formatType = FormatType.AdpcmWaveFormat;
        }

        protected override void SetDataSize(uint value) { }

        protected override void SetData(byte[] values)
        {
            data = values;
        }

        // XNB における LoopStart/LoopLength は、
        // XAUDIO2_BUFFER.LoopBegin/LoopLength の指定のみならず、
        // XAUDIO2_BUFFER.PlayBegin/PlayLength の指定を兼ねていると思われる。
        // 実際には、XNB ビルド時に再生範囲指定を行えないため、
        // LoopStart/LoopLength はバッファ全体の再生となるように、
        // サンプル辺りのバイト数から算出された値が設定されていると推測する。
        // XAUDIO2_BUFFER.PlayBegin/PlayLength を共に 0 とすることで、
        // XAudio2 はバッファ全体を再生し、また、
        // 全体ループならば WAVE フォーマットの BitsPerSample により
        // XAUDIO2_BUFFER.LoopBegin/LoopLength に対して適切な値を設定する事ができるため、
        // 明示的に SoundEffect へ設定する必要があると思えないが、
        // 念のため XNB に含まれる値を SoundEffect へ設定している。

        protected override void SetLoopStart(int value)
        {
            audioBuffer.PlayBegin = value;
        }

        protected override void SetLoopLength(int value)
        {
            audioBuffer.PlayLength = value;
        }

        // フォーマット情報から算出できるため duration は取り扱わない。
        protected override void SetDuration(int value) { }

        protected override void Begin()
        {
            // TODO
            // 当面、デフォルト マネージャから生成。
            audioBuffer = SoundManager.Default.CreateAudioBuffer();
        }

        protected override object End()
        {
            switch (formatType)
            {
                case FormatType.WaveFormat:
                    audioBuffer.Initialize(waveFormat, data, 0, data.Length);
                    break;
                case FormatType.WaveFormatExtensible:
                    audioBuffer.Initialize(waveFormatExtensible, data, 0, data.Length);
                    break;
                case FormatType.AdpcmWaveFormat:
                    audioBuffer.Initialize(adpcmWaveFormat, data, 0, data.Length);
                    break;
            }

            return new SoundEffect(SoundManager.Default, audioBuffer);
        }
    }
}
