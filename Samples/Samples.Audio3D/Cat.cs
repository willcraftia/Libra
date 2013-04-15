#region Using

using System;
using Libra;
using Libra.Games;

#endregion

namespace Samples.Audio3D
{
    public sealed class Cat : SpriteEntity
    {
        TimeSpan timeDelay = TimeSpan.Zero;

        static Random random = new Random();

        public override void Update(GameTime gameTime, AudioComponent audioManager)
        {
            var time = gameTime.TotalGameTime.TotalSeconds;

            var dx = (float) -Math.Cos(time);
            var dz = (float) -Math.Sin(time);

            var newPosition = new Vector3(dx, 0, dz) * 6000;

            Velocity = newPosition - Position;
            Position = newPosition;
            if (Velocity == Vector3.Zero)
                Forward = Vector3.Forward;
            else
                Forward = Vector3.Normalize(Velocity);

            Up = Vector3.Up;

            timeDelay -= gameTime.ElapsedGameTime;

            if (timeDelay < TimeSpan.Zero)
            {
                var soundName = "CatSound" + random.Next(3);

                audioManager.Play3DSound(soundName, false, this);

                timeDelay += TimeSpan.FromSeconds(1.25f);
            }
        }
    }
}
