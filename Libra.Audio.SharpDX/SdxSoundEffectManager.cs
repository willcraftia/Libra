#region Using

using System;

using X3DAX3DAudio = SharpDX.X3DAudio.X3DAudio;
using XA2MasteringVoice = SharpDX.XAudio2.MasteringVoice;
using XA2XAudio2 = SharpDX.XAudio2.XAudio2;
using SDXMSpeakers = SharpDX.Multimedia.Speakers;

#endregion

namespace Libra.Audio.SharpDX
{
    public sealed class SdxSoundEffectManager : SoundEffectManager
    {
        bool engineStarted;

        public XA2XAudio2 XAudio2 { get; private set; }

        public X3DAX3DAudio X3DAudio { get; private set; }

        public SDXMSpeakers Speakers { get; private set; }

        public XA2MasteringVoice MasteringVoice { get; private set; }

        public SdxSoundEffectManager()
        {
            XAudio2 = new XA2XAudio2();
            XAudio2.StartEngine();
            engineStarted = true;

            Speakers = XAudio2.GetDeviceDetails(0).OutputFormat.ChannelMask;
            X3DAudio = new X3DAX3DAudio(Speakers);
            
            MasteringVoice = new XA2MasteringVoice(XAudio2);
            MasteringVoice.SetVolume(MasterVolume);
        }

        public override SoundEffect CreateSoundEffect()
        {
            return new SdxSoundEffect(this);
        }

        protected override SoundEffectInstance CreateSoundEffectInstanceCore()
        {
            return new SdxSoundEffectInstance(this);
        }

        protected override void OnMasterVolumeChanged()
        {
            MasteringVoice.SetVolume(MasterVolume);
        }

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                if (engineStarted)
                {
                    // 破棄の前にエンジンを停止させる必要はないと思われるが、
                    // 念のため停止。
                    // StopEngine() は出力を即時停止しているのみであり、
                    // StartEngine() の再呼び出しにより再開可能な状態でしかない。
                    XAudio2.StopEngine();
                    engineStarted = false;
                }

                MasteringVoice.DestroyVoice();
                MasteringVoice.Dispose();

                // XAudio2 に関連付けられた全ての Voice を除去していない場合、
                // XAudio2.Dispose() はキューにある全バッファの消費を待機しているように見える。
                // 即座に破棄するには、先行して全ての Voice を除去する必要があると思われる。
                // このため、マネージャの破棄では、
                // マネージャから生成する SoundEffectInstance の参照を保持し、
                // それらが保持する SourceVoice について DestroyVoice() を
                // 先行して呼び出す手順としている。
                // しかし、この手順は明示的な Dispose() 呼び出しでのみ保証でき、
                // SharpDX 利用側クラスにとっての XAudio2 がマネージドの SharpDX クラスである以上は、
                // GC からのファイナライザ呼び出しでは保証できない。

                XAudio2.Dispose();
            }

            base.DisposeOverride(disposing);
        }
    }
}
