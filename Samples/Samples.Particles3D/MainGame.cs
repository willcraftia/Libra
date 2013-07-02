#region Using

using System;
using System.Collections.Generic;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Graphics.Toolkit;
using Libra.Input;
using Libra.Xnb;

#endregion

namespace Samples.Particles3D
{
    public sealed class MainGame : Game
    {
        #region ParticleState

        enum ParticleState
        {
            Explosions,
            SmokePlume,
            RingOfFire,
        }

        #endregion

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
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(DeviceContext);
            font = content.Load<SpriteFont>("font");
            grid = content.Load<Model>("grid");

            BuildExplosionParticleSystem();
            BuildExplosionSmokeParticleSystem();
            BuildProjectileTrailParticleSystem();
            BuildSmokePlumeParticleSystem();
            BuildFireParticleSystem();
        }

        void BuildExplosionParticleSystem()
        {
            explosionParticles = new ParticleSystem(DeviceContext, 100);

            explosionParticles.Texture = content.Load<Texture2D>("explosion");

            explosionParticles.Duration = 2;
            explosionParticles.DurationRandomness = 1;

            explosionParticles.MinHorizontalVelocity = 20;
            explosionParticles.MaxHorizontalVelocity = 30;

            explosionParticles.MinVerticalVelocity = -20;
            explosionParticles.MaxVerticalVelocity = 20;

            explosionParticles.EndVelocity = 0;

            explosionParticles.MinColor = Color.DarkGray.ToVector4();
            explosionParticles.MaxColor = Color.Gray.ToVector4();

            explosionParticles.MinRotateSpeed = -1;
            explosionParticles.MaxRotateSpeed = 1;

            explosionParticles.MinStartSize = 7;
            explosionParticles.MaxStartSize = 7;

            explosionParticles.MinEndSize = 70;
            explosionParticles.MaxEndSize = 140;

            explosionParticles.BlendState = BlendState.Additive;
        }

        void BuildExplosionSmokeParticleSystem()
        {
            explosionSmokeParticles = new ParticleSystem(DeviceContext, 200);

            explosionSmokeParticles.Texture = content.Load<Texture2D>("smoke");

            explosionSmokeParticles.Duration = 4;

            explosionSmokeParticles.MinHorizontalVelocity = 0;
            explosionSmokeParticles.MaxHorizontalVelocity = 50;

            explosionSmokeParticles.MinVerticalVelocity = -10;
            explosionSmokeParticles.MaxVerticalVelocity = 50;

            explosionSmokeParticles.Gravity = new Vector3(0, -20, 0);

            explosionSmokeParticles.EndVelocity = 0;

            explosionSmokeParticles.MinColor = Color.LightGray.ToVector4();
            explosionSmokeParticles.MaxColor = Color.White.ToVector4();

            explosionSmokeParticles.MinRotateSpeed = -2;
            explosionSmokeParticles.MaxRotateSpeed = 2;

            explosionSmokeParticles.MinStartSize = 7;
            explosionSmokeParticles.MaxStartSize = 7;

            explosionSmokeParticles.MinEndSize = 70;
            explosionSmokeParticles.MaxEndSize = 140;
        }

        void BuildProjectileTrailParticleSystem()
        {
            projectileTrailParticles = new ParticleSystem(DeviceContext, 1000);

            projectileTrailParticles.Texture = content.Load<Texture2D>("smoke");

            projectileTrailParticles.Duration = 3;

            projectileTrailParticles.DurationRandomness = 1.5f;

            projectileTrailParticles.EmitterVelocitySensitivity = 0.1f;

            projectileTrailParticles.MinHorizontalVelocity = 0;
            projectileTrailParticles.MaxHorizontalVelocity = 1;

            projectileTrailParticles.MinVerticalVelocity = -1;
            projectileTrailParticles.MaxVerticalVelocity = 1;

            projectileTrailParticles.MinColor = new Color(64, 96, 128, 255).ToVector4();
            projectileTrailParticles.MaxColor = new Color(255, 255, 255, 128).ToVector4();

            projectileTrailParticles.MinRotateSpeed = -4;
            projectileTrailParticles.MaxRotateSpeed = 4;

            projectileTrailParticles.MinStartSize = 1;
            projectileTrailParticles.MaxStartSize = 3;

            projectileTrailParticles.MinEndSize = 4;
            projectileTrailParticles.MaxEndSize = 11;
        }

        void BuildSmokePlumeParticleSystem()
        {
            smokePlumeParticles = new ParticleSystem(DeviceContext, 600);

            smokePlumeParticles.Texture = content.Load<Texture2D>("smoke");

            smokePlumeParticles.Duration = 10;

            smokePlumeParticles.MinHorizontalVelocity = 0;
            smokePlumeParticles.MaxHorizontalVelocity = 15;

            smokePlumeParticles.MinVerticalVelocity = 10;
            smokePlumeParticles.MaxVerticalVelocity = 20;

            smokePlumeParticles.Gravity = new Vector3(-20, -5, 0);

            smokePlumeParticles.EndVelocity = 0.75f;

            smokePlumeParticles.MinRotateSpeed = -1;
            smokePlumeParticles.MaxRotateSpeed = 1;

            smokePlumeParticles.MinStartSize = 4;
            smokePlumeParticles.MaxStartSize = 7;

            smokePlumeParticles.MinEndSize = 35;
            smokePlumeParticles.MaxEndSize = 140;
        }

        void BuildFireParticleSystem()
        {
            fireParticles = new ParticleSystem(DeviceContext, 2400);

            fireParticles.Texture = content.Load<Texture2D>("fire");

            fireParticles.Duration = 2;

            fireParticles.DurationRandomness = 1;

            fireParticles.MinHorizontalVelocity = 0;
            fireParticles.MaxHorizontalVelocity = 15;

            fireParticles.MinVerticalVelocity = -10;
            fireParticles.MaxVerticalVelocity = 10;

            fireParticles.Gravity = new Vector3(0, 15, 0);

            fireParticles.MinColor = new Color(255, 255, 255, 10).ToVector4();
            fireParticles.MaxColor = new Color(255, 255, 255, 40).ToVector4();

            fireParticles.MinStartSize = 5;
            fireParticles.MaxStartSize = 10;

            fireParticles.MinEndSize = 10;
            fireParticles.MaxEndSize = 40;

            fireParticles.BlendState = BlendState.Additive;
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

            smokePlumeParticles.Update(gameTime.ElapsedGameTime);
            explosionSmokeParticles.Update(gameTime.ElapsedGameTime);
            projectileTrailParticles.Update(gameTime.ElapsedGameTime);
            explosionParticles.Update(gameTime.ElapsedGameTime);
            fireParticles.Update(gameTime.ElapsedGameTime);

            base.Update(gameTime);
        }

        void UpdateExplosions(GameTime gameTime)
        {
            timeToNextProjectile -= gameTime.ElapsedGameTime;

            if (timeToNextProjectile <= TimeSpan.Zero)
            { 
                projectiles.Add(
                    new Projectile(explosionParticles, explosionSmokeParticles, projectileTrailParticles));

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
            DeviceContext.Clear(Color.CornflowerBlue);

            Matrix view = Matrix.CreateTranslation(0, -25, 0) *
                          Matrix.CreateRotationY(MathHelper.ToRadians(cameraRotation)) *
                          Matrix.CreateRotationX(MathHelper.ToRadians(cameraArc)) *
                          Matrix.CreateLookAt(new Vector3(0, 0, -cameraDistance),
                                              new Vector3(0, 0, 0), Vector3.Up);

            Matrix projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, DeviceContext.Viewport.AspectRatio, 1, 10000);

            explosionParticles.View = view;
            explosionParticles.Projection = projection;
            explosionSmokeParticles.View = view;
            explosionSmokeParticles.Projection = projection;
            projectileTrailParticles.View = view;
            projectileTrailParticles.Projection = projection;
            smokePlumeParticles.View = view;
            smokePlumeParticles.Projection = projection;
            fireParticles.View = view;
            fireParticles.Projection = projection;

            DrawGrid(view, projection);

            smokePlumeParticles.Draw();
            explosionSmokeParticles.Draw();
            projectileTrailParticles.Draw();
            explosionParticles.Draw();
            fireParticles.Draw();

            DrawMessage();

            base.Draw(gameTime);
        }

        void DrawGrid(Matrix view, Matrix projection)
        {
            DeviceContext.BlendState = BlendState.Opaque;
            DeviceContext.DepthStencilState = DepthStencilState.Default;
            DeviceContext.PixelShaderSamplers[0] = SamplerState.LinearWrap;

            grid.Draw(Matrix.Identity, view, projection);
        }

        void DrawMessage()
        {
            string message = string.Format(
                "Current effect: {0}!!!\n" +
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
            {
                cameraDistance = 500;
            }
            else if (cameraDistance < 10)
            {
                cameraDistance = 10;
            }

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
