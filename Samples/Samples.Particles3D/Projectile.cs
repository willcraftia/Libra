#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics.Toolkit;

#endregion

namespace Samples.Particles3D
{
    public sealed class Projectile
    {
        const float trailParticlesPerSecond = 200;

        const int numExplosionParticles = 30;
        
        const int numExplosionSmokeParticles = 50;
        
        const float projectileLifespan = 1.5f;
        
        const float sidewaysVelocityRange = 60;
        
        const float verticalVelocityRange = 40;
        
        const float gravity = 15;

        ParticleSystem explosionParticles;
        
        ParticleSystem explosionSmokeParticles;
        
        ParticleEmitter trailEmitter;

        Vector3 position;
        
        Vector3 velocity;
        
        float age;

        static Random random = new Random();
        
        public Projectile(ParticleSystem explosionParticles,
                          ParticleSystem explosionSmokeParticles,
                          ParticleSystem projectileTrailParticles)
        {
            this.explosionParticles = explosionParticles;
            this.explosionSmokeParticles = explosionSmokeParticles;

            position = Vector3.Zero;

            velocity.X = (float) (random.NextDouble() - 0.5) * sidewaysVelocityRange;
            velocity.Y = (float) (random.NextDouble() + 0.5) * verticalVelocityRange;
            velocity.Z = (float) (random.NextDouble() - 0.5) * sidewaysVelocityRange;

            trailEmitter = new ParticleEmitter(projectileTrailParticles,
                                               trailParticlesPerSecond, position);
        }

        public bool Update(GameTime gameTime)
        {
            float elapsedSeconds = (float) gameTime.ElapsedGameTime.TotalSeconds;

            position += velocity * elapsedSeconds;
            velocity.Y -= elapsedSeconds * gravity;
            age += elapsedSeconds;

            trailEmitter.Update(gameTime.ElapsedGameTime, position);

            if (age > projectileLifespan)
            {
                for (int i = 0; i < numExplosionParticles; i++)
                    explosionParticles.AddParticle(position, velocity);

                for (int i = 0; i < numExplosionSmokeParticles; i++)
                    explosionSmokeParticles.AddParticle(position, velocity);

                return false;
            }

            return true;
        }
    }
}
