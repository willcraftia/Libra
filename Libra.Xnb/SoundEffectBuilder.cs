﻿#region Using

using System;
using System.Runtime.InteropServices;
using Felis;
using Libra.Audio;

#endregion

namespace Libra.Xnb
{
    public sealed class SoundEffectBuilder : SoundEffectBuilderBase<SoundEffect>
    {
        enum FormatType
        {
            WaveFormat,
            WaveFormatExtensible,
            AdpcmWaveFormat
        }

        SoundEffect instance;

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

        protected override void SetLoopStart(int value)
        {
            instance.LoopStart = value;
        }

        protected override void SetLoopLength(int value)
        {
            instance.LoopLength = value;
        }

        // フォーマット情報から算出できるため duration は取り扱わない。
        protected override void SetDuration(int value) { }

        protected override void Begin()
        {
            // TODO
            // 当面、デフォルト マネージャから生成。
            instance = SoundEffectManager.Default.CreateSoundEffect();
        }

        protected override object End()
        {
            switch (formatType)
            {
                case FormatType.WaveFormat:
                    instance.Initialize(waveFormat, data, 0, data.Length);
                    break;
                case FormatType.WaveFormatExtensible:
                    instance.Initialize(waveFormatExtensible, data, 0, data.Length);
                    break;
                case FormatType.AdpcmWaveFormat:
                    instance.Initialize(adpcmWaveFormat, data, 0, data.Length);
                    break;
            }

            return instance;
        }
    }
}