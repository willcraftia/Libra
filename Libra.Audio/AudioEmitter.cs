#region Using

using System;

#endregion

namespace Libra.Audio
{
    public sealed class AudioEmitter
    {
        float dopplerScale;

        public float DopplerScale
        {
            get { return dopplerScale; }
            set
            {
                if (value < 0.0f) throw new ArgumentOutOfRangeException("value");

                dopplerScale = value;
            }
        }

        public Vector3 Forward { get; set; }

        public Vector3 Position { get; set; }

        public Vector3 Up { get; set; }

        public Vector3 Velocity { get; set; }

        public AudioEmitter()
        {
            dopplerScale = 1.0f;
            Forward = Vector3.Forward;
            Position = Vector3.Zero;
            Up = Vector3.Up;
            Velocity = Vector3.Zero;
        }
    }
}
