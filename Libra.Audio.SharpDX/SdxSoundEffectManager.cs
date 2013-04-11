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

        protected override SoundEffect CreateSoundEffect()
        {
            return new SdxSoundEffect(this);
        }

        protected override void OnMasterVolumeChanged()
        {
            MasteringVoice.SetVolume(MasterVolume);
        }

        protected override void DisposeOverride(bool disposing)
        {
            if (engineStarted)
            {
                XAudio2.StopEngine();
                engineStarted = false;
            }

            base.DisposeOverride(disposing);
        }
    }
}
