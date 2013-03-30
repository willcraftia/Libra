#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Xnb;

#endregion

namespace Samples.Particles3D.ParticleSystems
{
    sealed class ExplosionParticleSystem : ParticleSystem
    {
        public ExplosionParticleSystem(Game game, XnbManager content)
            : base(game, content)
        {
        }

        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "explosion";

            settings.MaxParticles = 100;

            settings.Duration = TimeSpan.FromSeconds(2);
            settings.DurationRandomness = 1;

            settings.MinHorizontalVelocity = 20;
            settings.MaxHorizontalVelocity = 30;

            settings.MinVerticalVelocity = -20;
            settings.MaxVerticalVelocity = 20;

            settings.EndVelocity = 0;

            settings.MinColor = Color.DarkGray;
            settings.MaxColor = Color.Gray;

            settings.MinRotateSpeed = -1;
            settings.MaxRotateSpeed = 1;

            settings.MinStartSize = 7;
            settings.MaxStartSize = 7;

            settings.MinEndSize = 70;
            settings.MaxEndSize = 140;

            settings.BlendState = BlendState.Additive;
        }
    }
}
