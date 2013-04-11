#region Using

using System;

#endregion

namespace Libra.Audio
{
    public sealed class AudioListener
    {
        public Vector3 Forward { get; set; }

        public Vector3 Position { get; set; }

        public Vector3 Up { get; set; }

        public Vector3 Velocity { get; set; }

        public AudioListener()
        {
            Forward = Vector3.Forward;
            Position = Vector3.Zero;
            Up = Vector3.Up;
            Velocity = Vector3.Zero;
        }
    }
}
