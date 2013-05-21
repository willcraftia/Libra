#region Using

using System;

#endregion

// Xbox LIVE Indie Games - Particles 3D より移植。
// http://xbox.create.msdn.com/en-US/education/catalog/sample/particle_3d

namespace Libra.Graphics.Toolkit
{
    public sealed class ParticleEmitter
    {
        ParticleSystem particleSystem;

        float timeBetweenParticles;

        Vector3 previousPosition;

        float timeLeftOver;

        public ParticleEmitter(ParticleSystem particleSystem, float particlesPerSecond, Vector3 initialPosition)
        {
            if (particleSystem == null) throw new ArgumentNullException("particleSystem");
            if (particlesPerSecond <= 0.0f) throw new ArgumentOutOfRangeException("particlesPerSecond");

            this.particleSystem = particleSystem;

            timeBetweenParticles = 1.0f / particlesPerSecond;

            previousPosition = initialPosition;
        }

        public void Update(TimeSpan elapsedGameTime, Vector3 newPosition)
        {
            float elapsedTime = (float) elapsedGameTime.TotalSeconds;

            if (0 < elapsedTime)
            {
                Vector3 velocity = (newPosition - previousPosition) / elapsedTime;

                float timeToSpend = timeLeftOver + elapsedTime;

                float currentTime = -timeLeftOver;

                while (timeToSpend > timeBetweenParticles)
                {
                    currentTime += timeBetweenParticles;
                    timeToSpend -= timeBetweenParticles;

                    float mu = currentTime / elapsedTime;

                    Vector3 position = Vector3.Lerp(previousPosition, newPosition, mu);

                    particleSystem.AddParticle(position, velocity);
                }

                timeLeftOver = timeToSpend;
            }

            previousPosition = newPosition;
        }
    }
}
