#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Input;
using Libra.Xnb;

#endregion

namespace Samples.BloomPostprocess
{
    public sealed class MainGame : Game
    {
        GraphicsManager graphics;

        BloomComponent bloom;

        int bloomSettingsIndex = 0;

        SpriteBatch spriteBatch;
        
        SpriteFont spriteFont;
        
        Texture2D background;

        Model model;

        BasicEffect basicEffect;

        KeyboardState lastKeyboardState = new KeyboardState();

        JoystickState lastJoystickState = new JoystickState();
        
        KeyboardState currentKeyboardState = new KeyboardState();
        
        JoystickState currentJoystickState = new JoystickState();

        public XnbManager Content { get; private set; }

        public MainGame()
        {
            Content = new XnbManager(Services);
            Content.RootDirectory = "Content";

            graphics = new GraphicsManager(this);

            bloom = new BloomComponent(this);

            Components.Add(bloom);
        }

        protected override void LoadContent()
        {
            basicEffect = new BasicEffect(Device);
            spriteBatch = new SpriteBatch(Device);
            spriteFont = Content.Load<SpriteFont>("hudFont");
            background = Content.Load<Texture2D>("sunset");
            model = Content.Load<Model>("tank");
        }

        protected override void Update(GameTime gameTime)
        {
            HandleInput();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            var context = Device.ImmediateContext;
            var viewport = context.Viewport;

            bloom.BeginDraw();

            context.Clear(Color.Black);

            spriteBatch.Begin(context, SpriteSortMode.Deferred, BlendState.Opaque);
            spriteBatch.Draw(background.GetShaderResourceView(), new Rectangle(0, 0, (int) viewport.Width, (int) viewport.Height), Color.White);
            spriteBatch.End();

            context.DepthStencilState = DepthStencilState.Default;

            DrawModel(gameTime);

            base.Draw(gameTime);
 
            DrawOverlayText();
        }

        void DrawModel(GameTime gameTime)
        {
            var context = Device.ImmediateContext;

            float time = (float)gameTime.TotalGameTime.TotalSeconds;

            var viewport = context.Viewport;
            float aspectRatio = (float)viewport.Width / (float)viewport.Height;

            var world = Matrix.CreateRotationY(time * 0.42f);
            var view = Matrix.CreateLookAt(new Vector3(750, 100, 0), new Vector3(0, 300, 0), Vector3.Up);
            var projection = Matrix.CreatePerspectiveFieldOfView(1, aspectRatio, 1, 10000);
 
            var transforms = new Matrix[model.Bones.Count];

            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (var mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = transforms[mesh.ParentBone.Index] * world;
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();

                    effect.SpecularColor = Vector3.One;
                }

                mesh.Draw(context);
            }
        }

        void DrawOverlayText()
        {
            var text = "A = settings (" + bloom.Settings.Name + ")\n" +
                       "B = toggle bloom (" + (bloom.Visible ? "on" : "off") + ")\n" +
                       "X = show buffer (" + bloom.ShowBuffer.ToString() + ")";

            spriteBatch.Begin(Device.ImmediateContext);

            spriteBatch.DrawString(spriteFont, text, new Vector2(65, 65), Color.Black);
            spriteBatch.DrawString(spriteFont, text, new Vector2(64, 64), Color.White);

            spriteBatch.End();
        }

        void HandleInput()
        {
            lastKeyboardState = currentKeyboardState;
            lastJoystickState = currentJoystickState;

            currentKeyboardState = Keyboard.GetState();
            currentJoystickState = Joystick.GetState();
 
            if (currentKeyboardState.IsKeyDown(Keys.Escape) ||
                currentJoystickState.Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }

            if ((currentJoystickState.Buttons.A == ButtonState.Pressed &&
                 lastJoystickState.Buttons.A != ButtonState.Pressed) ||
                (currentKeyboardState.IsKeyDown(Keys.A) &&
                 lastKeyboardState.IsKeyUp(Keys.A)))
            {
                bloomSettingsIndex = (bloomSettingsIndex + 1) %
                                     BloomSettings.PresetSettings.Length;
             
                bloom.Settings = BloomSettings.PresetSettings[bloomSettingsIndex];
                bloom.Visible = true;
            }

            if ((currentJoystickState.Buttons.B == ButtonState.Pressed &&
                 lastJoystickState.Buttons.B != ButtonState.Pressed) ||
                (currentKeyboardState.IsKeyDown(Keys.B) &&
                 lastKeyboardState.IsKeyUp(Keys.B)))
            {
                bloom.Visible = !bloom.Visible;
            }

            if ((currentJoystickState.Buttons.X == ButtonState.Pressed &&
                 lastJoystickState.Buttons.X != ButtonState.Pressed) ||
                (currentKeyboardState.IsKeyDown(Keys.X) &&
                 lastKeyboardState.IsKeyUp(Keys.X)))
            {
                bloom.Visible = true;
                bloom.ShowBuffer++;

                if (bloom.ShowBuffer > BloomComponent.IntermediateBuffer.FinalResult)
                    bloom.ShowBuffer= 0;
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
