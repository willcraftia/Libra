#region Using

using System;
using Libra;
using Libra.Audio;
using Libra.Games;

#endregion

namespace Samples.Audio3D
{
    public sealed class Dog : SpriteEntity
    {
        TimeSpan timeDelay = TimeSpan.Zero;

        StaticSound activeSound = null;

        public override void Update(GameTime gameTime, AudioComponent audioManager)
        {
            Position = new Vector3(0, 0, -4000);
            Forward = Vector3.Forward;
            Up = Vector3.Up;
            Velocity = Vector3.Zero;

            timeDelay -= gameTime.ElapsedGameTime;

            if (timeDelay < TimeSpan.Zero)
            {
                if (activeSound == null)
                {
                    activeSound = audioManager.Play3DSound("DogSound", true, this);

                    timeDelay += TimeSpan.FromSeconds(6);
                }
                else
                {
                    activeSound.Stop(false);
                    activeSound = null;

                    timeDelay += TimeSpan.FromSeconds(4);
                }
            }
        }
    }
}
