#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Games.SharpDX;
using Libra.Graphics;
using Libra.Input;
using Libra.Xnb;

#endregion

namespace Samples.LensFlare
{
    public sealed class MainGame : Game
    {
        IGamePlatform platform;

        GraphicsManager graphics;

        IKeyboard keyboard;

        IJoystick joystick;

        KeyboardState currentKeyboardState = new KeyboardState();

        JoystickState currentJoystickState = new JoystickState();
        
        Vector3 cameraPosition = new Vector3(-200, 30, 30);
        
        Vector3 cameraFront = new Vector3(1, 0, 0);

        Model terrain;

        LensFlareComponent lensFlare;

        internal XnbManager Content { get; private set; }

        public MainGame()
        {
            platform = new SdxFormGamePlatform(this)
            {
                DirectInputEnabled = true
            };
            graphics = new GraphicsManager(this);

            Content = new XnbManager(Services, "Content");

            lensFlare = new LensFlareComponent(this);

            Components.Add(lensFlare);
        }

        protected override void LoadContent()
        {
            terrain = Content.Load<Model>("terrain");

            keyboard = platform.CreateKeyboard();
            joystick = platform.CreateJoystick();
        }

        protected override void Update(GameTime gameTime)
        {
            HandleInput();

            UpdateCamera(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            var context = Device.ImmediateContext;

            context.Clear(Color.CornflowerBlue);

            var view = Matrix.CreateLookAt(cameraPosition, cameraPosition + cameraFront, Vector3.Up);

            var aspectRatio = context.Viewport.AspectRatio;
            var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.1f, 500);

            context.RasterizerState = RasterizerState.CullNone;

            foreach (var mesh in terrain.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = Matrix.Identity;
                    effect.View = view;
                    effect.Projection = projection;

                    effect.LightingEnabled = true;
                    effect.DiffuseColor = new Vector3(1f);
                    effect.AmbientLightColor = new Vector3(0.5f);

                    effect.DirectionalLights[0].Enabled = true;
                    effect.DirectionalLights[0].DiffuseColor = Vector3.One;
                    effect.DirectionalLights[0].Direction = lensFlare.LightDirection;

                    effect.FogEnabled = true;
                    effect.FogStart = 200;
                    effect.FogEnd = 500;
                    effect.FogColor = Color.CornflowerBlue.ToVector3();
                }

                mesh.Draw(context);
            }

            lensFlare.View = view;
            lensFlare.Projection = projection;

            base.Draw(gameTime);
        }

        void HandleInput()
        {
            currentKeyboardState = keyboard.GetState();
            currentJoystickState = joystick.GetState();

            if (currentKeyboardState.IsKeyDown(Keys.Escape) ||
                currentJoystickState.Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }
        }

        void UpdateCamera(GameTime gameTime)
        {
            float time = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            float pitch = -currentJoystickState.ThumbSticks.Right.Y * time * 0.001f;
            float turn = -currentJoystickState.ThumbSticks.Right.X * time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Up))
                pitch += time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Down))
                pitch -= time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Left))
                turn += time * 0.001f;

            if (currentKeyboardState.IsKeyDown(Keys.Right))
                turn -= time * 0.001f;

            var cameraRight = Vector3.Cross(Vector3.Up, cameraFront);
            var flatFront = Vector3.Cross(cameraRight, Vector3.Up);

            var pitchMatrix = Matrix.CreateFromAxisAngle(cameraRight, pitch);
            var turnMatrix = Matrix.CreateFromAxisAngle(Vector3.Up, turn);

            var tiltedFront = Vector3.TransformNormal(cameraFront, pitchMatrix * turnMatrix);

            if (Vector3.Dot(tiltedFront, flatFront) > 0.001f)
            {
                cameraFront = Vector3.Normalize(tiltedFront);
            }

            if (currentKeyboardState.IsKeyDown(Keys.W))
                cameraPosition += cameraFront * time * 0.1f;
            
            if (currentKeyboardState.IsKeyDown(Keys.S))
                cameraPosition -= cameraFront * time * 0.1f;

            if (currentKeyboardState.IsKeyDown(Keys.A))
                cameraPosition += cameraRight * time * 0.1f;

            if (currentKeyboardState.IsKeyDown(Keys.D))
                cameraPosition -= cameraRight * time * 0.1f;

            cameraPosition += cameraFront * currentJoystickState.ThumbSticks.Left.Y * time * 0.1f;
            cameraPosition -= cameraRight * currentJoystickState.ThumbSticks.Left.X * time * 0.1f;

            if (currentJoystickState.Buttons.RightStick == ButtonState.Pressed ||
                currentKeyboardState.IsKeyDown(Keys.R))
            {
                cameraPosition = new Vector3(-200, 30, 30);
                cameraFront = new Vector3(1, 0, 0);
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
