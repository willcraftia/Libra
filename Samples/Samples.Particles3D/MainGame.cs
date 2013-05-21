#region Using

using System;
using System.Collections.Generic;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Input;
using Libra.Xnb;
using Samples.Particles3D.ParticleSystems;

#endregion

namespace Samples.Particles3D
{
    public sealed class MainGame : Game
    {
        GraphicsManager graphics;

        XnbManager content;

        SpriteBatch spriteBatch;

        SpriteFont font;

        Model grid;

        ParticleSystem explosionParticles;
        
        ParticleSystem explosionSmokeParticles;
        
        ParticleSystem projectileTrailParticles;
        
        ParticleSystem smokePlumeParticles;
        
        ParticleSystem fireParticles;

        enum ParticleState
        {
            Explosions,
            SmokePlume,
            RingOfFire,
        };

        ParticleState currentState = ParticleState.Explosions;

        List<Projectile> projectiles = new List<Projectile>();

        TimeSpan timeToNextProjectile = TimeSpan.Zero;

        Random random = new Random();

        KeyboardState currentKeyboardState;
        
        JoystickState currentGamePadState;

        KeyboardState lastKeyboardState;
        
        JoystickState lastGamePadState;

        float cameraArc = -5;
        
        float cameraRotation = 0;
        
        float cameraDistance = 200;

        public MainGame()
        {
            graphics = new GraphicsManager(this);

            content = new XnbManager(Services, "Content");

            explosionParticles = new ExplosionParticleSystem(this, content);
            explosionSmokeParticles = new ExplosionSmokeParticleSystem(this, content);
            projectileTrailParticles = new ProjectileTrailParticleSystem(this, content);
            smokePlumeParticles = new SmokePlumeParticleSystem(this, content);
            fireParticles = new FireParticleSystem(this, content);

            smokePlumeParticles.DrawOrder = 100;
            explosionSmokeParticles.DrawOrder = 200;
            projectileTrailParticles.DrawOrder = 300;
            explosionParticles.DrawOrder = 400;
            fireParticles.DrawOrder = 500;
 
            Components.Add(explosionParticles);
            Components.Add(explosionSmokeParticles);
            Components.Add(projectileTrailParticles);
            Components.Add(smokePlumeParticles);
            Components.Add(fireParticles);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(Device);
            font = content.Load<SpriteFont>("font");
            grid = content.Load<Model>("grid");
        }

        protected override void Update(GameTime gameTime)
        {
            HandleInput();

            UpdateCamera(gameTime);

            switch (currentState)
            {
                case ParticleState.Explosions:
                    UpdateExplosions(gameTime);
                    break;

                case ParticleState.SmokePlume:
                    UpdateSmokePlume();
                    break;

                case ParticleState.RingOfFire:
                    UpdateFire();
                    break;
            }

            UpdateProjectiles(gameTime);

            base.Update(gameTime);
        }

        void UpdateExplosions(GameTime gameTime)
        {
            timeToNextProjectile -= gameTime.ElapsedGameTime;

            if (timeToNextProjectile <= TimeSpan.Zero)
            { 
                projectiles.Add(new Projectile(explosionParticles,
                                               explosionSmokeParticles,
                                               projectileTrailParticles));

                timeToNextProjectile += TimeSpan.FromSeconds(1);
            }
        }

        void UpdateProjectiles(GameTime gameTime)
        {
            int i = 0;

            while (i < projectiles.Count)
            {
                if (!projectiles[i].Update(gameTime))
                {
                    projectiles.RemoveAt(i);
                }
                else
                { 
                    i++;
                }
            }
        }

        void UpdateSmokePlume()
        {
            smokePlumeParticles.AddParticle(Vector3.Zero, Vector3.Zero);
        }

        void UpdateFire()
        {
            const int fireParticlesPerFrame = 20;
 
            for (int i = 0; i < fireParticlesPerFrame; i++)
            {
                fireParticles.AddParticle(RandomPointOnCircle(), Vector3.Zero);
            }

            smokePlumeParticles.AddParticle(RandomPointOnCircle(), Vector3.Zero);
        }

        Vector3 RandomPointOnCircle()
        {
            const float radius = 30;
            const float height = 40;

            double angle = random.NextDouble() * Math.PI * 2;

            float x = (float)Math.Cos(angle);
            float y = (float)Math.Sin(angle);

            return new Vector3(x * radius, y * radius + height, 0);
        }

        protected override void Draw(GameTime gameTime)
        {
            var context = Device.ImmediateContext;

            context.Clear(Color.CornflowerBlue);

            float aspectRatio = (float) context.Viewport.Width /
                                (float) context.Viewport.Height;

            Matrix view = Matrix.CreateTranslation(0, -25, 0) *
                          Matrix.CreateRotationY(MathHelper.ToRadians(cameraRotation)) *
                          Matrix.CreateRotationX(MathHelper.ToRadians(cameraArc)) *
                          Matrix.CreateLookAt(new Vector3(0, 0, -cameraDistance),
                                              new Vector3(0, 0, 0), Vector3.Up);

            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
                                                                    aspectRatio,
                                                                    1, 10000);

            explosionParticles.SetCamera(view, projection);
            explosionSmokeParticles.SetCamera(view, projection);
            projectileTrailParticles.SetCamera(view, projection);
            smokePlumeParticles.SetCamera(view, projection);
            fireParticles.SetCamera(view, projection);

            DrawGrid(view, projection);

            DrawMessage();

            base.Draw(gameTime);
        }

        void DrawGrid(Matrix view, Matrix projection)
        {
            var context = Device.ImmediateContext;

            context.BlendState = BlendState.Opaque;
            context.DepthStencilState = DepthStencilState.Default;
            context.PixelShaderSamplers[0] = SamplerState.LinearWrap;

            grid.Draw(Device.ImmediateContext, Matrix.Identity, view, projection);
        }

        void DrawMessage()
        {
            string message = string.Format("Current effect: {0}!!!\n" +
                                           "Hit the A button or space bar to switch.",
                                           currentState);

            spriteBatch.Begin();
            spriteBatch.DrawString(font, message, new Vector2(50, 50), Color.White);
            spriteBatch.End();
        }

        void HandleInput()
        {
            lastKeyboardState = currentKeyboardState;
            lastGamePadState = currentGamePadState;

            currentKeyboardState = Keyboard.GetState();
            currentGamePadState = Joystick.GetState();

            if (currentKeyboardState.IsKeyDown(Keys.Escape) ||
                currentGamePadState.Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }

            if ((currentKeyboardState.IsKeyDown(Keys.Space) &&
                 (lastKeyboardState.IsKeyUp(Keys.Space))) ||
                ((currentKeyboardState.IsKeyDown(Keys.A) &&
                 (lastKeyboardState.IsKeyUp(Keys.A))) ||
                ((currentGamePadState.Buttons.A == ButtonState.Pressed)) &&
                 (lastGamePadState.Buttons.A == ButtonState.Released)))
            {
                currentState++;

                if (currentState > ParticleState.RingOfFire)
                    currentState = 0;
            }
        }

        void UpdateCamera(GameTime gameTime)
        {
            float time = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            if (currentKeyboardState.IsKeyDown(Keys.Up))
            {
                cameraArc += time * 0.025f;
            }

            if (currentKeyboardState.IsKeyDown(Keys.Down))
            {
                cameraArc -= time * 0.025f;
            }

            cameraArc += currentGamePadState.ThumbSticks.Right.Y * time * 0.05f;

            if (cameraArc > 90.0f)
                cameraArc = 90.0f;
            else if (cameraArc < -90.0f)
                cameraArc = -90.0f;

            if (currentKeyboardState.IsKeyDown(Keys.Right))
            {
                cameraRotation += time * 0.05f;
            }

            if (currentKeyboardState.IsKeyDown(Keys.Left))
            {
                cameraRotation -= time * 0.05f;
            }

            cameraRotation += currentGamePadState.ThumbSticks.Right.X * time * 0.1f;

            if (currentKeyboardState.IsKeyDown(Keys.Z))
                cameraDistance += time * 0.25f;

            if (currentKeyboardState.IsKeyDown(Keys.X))
                cameraDistance -= time * 0.25f;

            cameraDistance += currentGamePadState.Triggers.Left * time * 0.5f;
            cameraDistance -= currentGamePadState.Triggers.Right * time * 0.5f;

            if (cameraDistance > 500)
                cameraDistance = 500;
            else if (cameraDistance < 10)
                cameraDistance = 10;

            if (currentGamePadState.Buttons.RightStick == ButtonState.Pressed ||
                currentKeyboardState.IsKeyDown(Keys.R))
            {
                cameraArc = -5;
                cameraRotation = 0;
                cameraDistance = 200;
            }
        }
    }

    #region Program

    static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new MainGame())
            {
                game.Run();
            }
        }
    }

    #endregion
}
