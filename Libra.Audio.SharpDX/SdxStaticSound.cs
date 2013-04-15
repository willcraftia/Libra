#region Using

using System;

using X3DACalculateFlags = SharpDX.X3DAudio.CalculateFlags;
using X3DAEmitter = SharpDX.X3DAudio.Emitter;
using X3DAListener = SharpDX.X3DAudio.Listener;
using XA2PlayFlags = SharpDX.XAudio2.PlayFlags;
using XA2SourceVoice = SharpDX.XAudio2.SourceVoice;
using XA2VoiceFlags = SharpDX.XAudio2.VoiceFlags;
using XA2XAudio2 = SharpDX.XAudio2.XAudio2;
using SDXMSpeakers = SharpDX.Multimedia.Speakers;

#endregion

namespace Libra.Audio.SharpDX
{
    public sealed class SdxStaticSound : StaticSound
    {
        float[] levelMatrices;

        public XA2SourceVoice SourceVoice { get; private set; }

        public SdxStaticSound(SdxSoundManager soundManager)
            : base(soundManager)
        {
        }

        protected override void InitializeCore()
        {
            var sdxAudioBuffer = AudioBuffer as SdxAudioBuffer;
            var sdxSoundManager = SoundManager as SdxSoundManager;

            SourceVoice = new XA2SourceVoice(
                sdxSoundManager.XAudio2,
                sdxAudioBuffer.WaveFormat,
                XA2VoiceFlags.None,
                XA2XAudio2.MaximumFrequencyRatio);

            SourceVoice.SetVolume(Volume);
        }

        protected override int GetBuffersQueued()
        {
            return SourceVoice.State.BuffersQueued;
        }

        protected override void Apply3DCore(AudioListener listener, AudioEmitter emitter)
        {
            var sdxAudioBuffer = AudioBuffer as SdxAudioBuffer;
            var sdxSoundManager = SoundManager as SdxSoundManager;

            var x3daListener = ToX3DAListener(listener);
            var x3daEmitter = ToX3DAEmitter(emitter);

            var sourceChannels = sdxAudioBuffer.WaveFormat.Channels;
            var destinationChannels = sdxSoundManager.MasteringVoice.VoiceDetails.InputChannelCount;

            var dpsSettings = sdxSoundManager.X3DAudio.Calculate(
                x3daListener,
                x3daEmitter,
                X3DACalculateFlags.Matrix | X3DACalculateFlags.Doppler,
                sourceChannels,
                destinationChannels);

            SourceVoice.SetOutputMatrix(
                sdxSoundManager.MasteringVoice,
                sourceChannels,
                destinationChannels,
                dpsSettings.MatrixCoefficients);

            SourceVoice.SetFrequencyRatio(dpsSettings.DopplerFactor);
        }

        protected override void PlayCore()
        {
            var sdxAudioBuffer = AudioBuffer as SdxAudioBuffer;

            var audioBuffer = IsLooped ? sdxAudioBuffer.LoopedAudioBuffer : sdxAudioBuffer.AudioBuffer;
            SourceVoice.SubmitSourceBuffer(audioBuffer, null);

            SourceVoice.Start();
        }

        protected override void PauseCore()
        {
            SourceVoice.Stop();
        }

        protected override void ResumeCore()
        {
            SourceVoice.Start();
        }

        protected override void StopCore(bool immediate)
        {
            SourceVoice.Stop(immediate ? (int) XA2PlayFlags.None : (int) XA2PlayFlags.Tails);

            if (immediate)
            {
                // 即時停止ならばキューに残存するバッファを削除。
                SourceVoice.FlushSourceBuffers();
            }
        }

        protected override void OnVolumeChanged()
        {
            SourceVoice.SetVolume(Volume);
        }

        protected override void OnPitchChanged()
        {
            SourceVoice.SetFrequencyRatio((float) Math.Pow(2.0, Pitch));
        }

        protected override void OnPanChanged()
        {
            var sdxAudioBuffer = AudioBuffer as SdxAudioBuffer;
            var sdxSoundManager = SoundManager as SdxSoundManager;

            var sourceChannels = sdxAudioBuffer.WaveFormat.Channels;
            var destinationChannels = sdxSoundManager.MasteringVoice.VoiceDetails.InputChannelCount;

            if (levelMatrices == null || levelMatrices.Length < destinationChannels)
                levelMatrices = new float[Math.Max(destinationChannels, 8)];

            for (int i = 0; i < levelMatrices.Length; i++)
                levelMatrices[i] = 1.0f;

            float left = 1.0f - Pan;
            float rigth = 1.0f + Pan;

            switch (sdxSoundManager.Speakers)
            {
                case SDXMSpeakers.Stereo:
                case SDXMSpeakers.TwoPointOne:
                case SDXMSpeakers.Surround:
                    levelMatrices[0] = left;
                    levelMatrices[1] = rigth;
                    break;
                case SDXMSpeakers.Quad:
                    levelMatrices[0] = levelMatrices[2] = left;
                    levelMatrices[1] = levelMatrices[3] = rigth;
                    break;
                case SDXMSpeakers.FivePointOne:
                case SDXMSpeakers.SevenPointOne:
                case SDXMSpeakers.FivePointOneSurround:
                    levelMatrices[0] = levelMatrices[4] = left;
                    levelMatrices[1] = levelMatrices[5] = rigth;
                    break;
                case SDXMSpeakers.SevenPointOneSurround:
                    levelMatrices[0] = levelMatrices[4] = levelMatrices[6] = left;
                    levelMatrices[1] = levelMatrices[5] = levelMatrices[4] = rigth;
                    break;
                case SDXMSpeakers.Mono:
                default:
                    break;
            }

            SourceVoice.SetOutputMatrix(sourceChannels, destinationChannels, levelMatrices);
        }

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                SourceVoice.DestroyVoice();
                SourceVoice.Dispose();
            }

            base.DisposeOverride(disposing);
        }

        X3DAListener ToX3DAListener(AudioListener listener)
        {
            var result = new X3DAListener();

            var position = listener.Position;
            position.Z *= -1.0f;

            result.Position.X = position.X;
            result.Position.Y = position.Y;
            result.Position.Z = position.Z;

            var velocity = listener.Velocity;
            velocity.Z *= 1.0f;

            result.Velocity.X = velocity.X;
            result.Velocity.Y = velocity.Y;
            result.Velocity.Z = velocity.Z;

            var orientFront = listener.Forward;
            orientFront *= -1.0f;

            result.OrientFront.X = orientFront.X;
            result.OrientFront.Y = orientFront.Y;
            result.OrientFront.Z = orientFront.Z;

            var orientTop = listener.Up;

            result.OrientTop.X = orientTop.X;
            result.OrientTop.Y = orientTop.Y;
            result.OrientTop.Z = orientTop.Z;

            return result;
        }

        X3DAEmitter ToX3DAEmitter(AudioEmitter emitter)
        {
            var result = new X3DAEmitter();

            var position = emitter.Position;
            position.Z *= -1.0f;

            result.Position.X = position.X;
            result.Position.Y = position.Y;
            result.Position.Z = position.Z;

            var velocity = emitter.Velocity;
            velocity.Z *= 1.0f;

            result.Velocity.X = velocity.X;
            result.Velocity.Y = velocity.Y;
            result.Velocity.Z = velocity.Z;

            var orientFront = emitter.Forward;
            orientFront *= -1.0f;

            result.OrientFront.X = orientFront.X;
            result.OrientFront.Y = orientFront.Y;
            result.OrientFront.Z = orientFront.Z;

            var orientTop = emitter.Up;

            result.OrientTop.X = orientTop.X;
            result.OrientTop.Y = orientTop.Y;
            result.OrientTop.Z = orientTop.Z;

            var sdxAudioBuffer = AudioBuffer as SdxAudioBuffer;
            var sdxSoundManager = SoundManager as SdxSoundManager;

            result.DopplerScaler = emitter.DopplerScale * sdxSoundManager.DopplerScale;
            result.ChannelCount = sdxAudioBuffer.WaveFormat.Channels;
            result.CurveDistanceScaler = sdxSoundManager.DistanceScale;

            return result;
        }
    }
}
