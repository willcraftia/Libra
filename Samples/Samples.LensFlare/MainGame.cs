#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Input;
using Libra.Xnb;

using TKLensFlare = Libra.Graphics.Toolkit.LensFlare;

#endregion

namespace Samples.LensFlare
{
    public sealed class MainGame : Game
    {
        #region FlareDefinition

        struct FlareDefinition
        {
            public float Position;

            public float Scale;

            public Color Color;

            public string TextureName;

            public FlareDefinition(float position, float scale, Color color, string textureName)
            {
                Position = position;
                Scale = scale;
                Color = color;
                TextureName = textureName;
            }
        }

        #endregion

        FlareDefinition[] flareDefinitions =
        {
            new FlareDefinition(-0.5f, 0.7f, new Color( 50,  25,  50), "flare1"),
            new FlareDefinition( 0.3f, 0.4f, new Color(100, 255, 200), "flare1"),
            new FlareDefinition( 1.2f, 1.0f, new Color(100,  50,  50), "flare1"),
            new FlareDefinition( 1.5f, 1.5f, new Color( 50, 100,  50), "flare1"),

            new FlareDefinition(-0.3f, 0.7f, new Color(200,  50,  50), "flare2"),
            new FlareDefinition( 0.6f, 0.9f, new Color( 50, 100,  50), "flare2"),
            new FlareDefinition( 0.7f, 0.4f, new Color( 50, 200, 200), "flare2"),

            new FlareDefinition(-0.7f, 0.7f, new Color( 50, 100,  25), "flare3"),
            new FlareDefinition( 0.0f, 0.6f, new Color( 25,  25,  25), "flare3"),
            new FlareDefinition( 2.0f, 1.4f, new Color( 25,  50, 100), "flare3"),
        };

        GraphicsManager graphics;

        KeyboardState currentKeyboardState = new KeyboardState();

        JoystickState currentJoystickState = new JoystickState();
        
        Vector3 cameraPosition = new Vector3(-200, 30, 30);
        
        Vector3 cameraFront = new Vector3(1, 0, 0);

        Model terrain;

        TKLensFlare lensFlare;

        internal XnbManager Content { get; private set; }

        public MainGame()
        {
            graphics = new GraphicsManager(this);

            Content = new XnbManager(Services, "Content");
        }

        protected override void LoadContent()
        {
            terrain = Content.Load<Model>("terrain");

            lensFlare = new TKLensFlare(DeviceContext);
            lensFlare.LightDirection = Vector3.Normalize(new Vector3(-1, -0.1f, 0.3f));

            foreach (var flareDefinition in flareDefinitions)
            {
                var flare = new TKLensFlare.Flare(
                    flareDefinition.Position,
                    flareDefinition.Scale,
                    flareDefinition.Color,
                    Content.Load<Texture2D>(flareDefinition.TextureName));

                lensFlare.Flares.Add(flare);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            HandleInput();

            UpdateCamera(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            DeviceContext.Clear(Color.CornflowerBlue);

            var view = Matrix.CreateLookAt(cameraPosition, cameraPosition + cameraFront, Vector3.Up);

            var aspectRatio = DeviceContext.Viewport.AspectRatio;
            var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.1f, 500);

            // D3D11 の TextureAddressMode のデフォルトは Clamp。
            // XNA (D3D9) はデフォルト Wrap を仮定しているため、ここで明示する必要がある。
            DeviceContext.PixelShaderSamplers[0] = SamplerState.LinearWrap;

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

                mesh.Draw();
            }

            DeviceContext.PixelShaderSamplers[0] = null;

            lensFlare.View = view;
            lensFlare.Projection = projection;
            lensFlare.Draw();

            base.Draw(gameTime);
        }

        void HandleInput()
        {
            currentKeyboardState = Keyboard.GetState();
            currentJoystickState = Joystick.GetState();

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
