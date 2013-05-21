#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Input;
using Libra.Xnb;

#endregion

namespace Samples.Audio3D
{
    public sealed class MainGame : Game
    {
        GraphicsManager graphics;

        AudioComponent audioManager;

        SpriteEntity cat;
        
        SpriteEntity dog;

        Texture2D checkerTexture;

        QuadDrawer quadDrawer;

        Vector3 cameraPosition = new Vector3(0, 512, 0);
        
        Vector3 cameraForward = Vector3.Forward;
        
        Vector3 cameraUp = Vector3.Up;
        
        Vector3 cameraVelocity = Vector3.Zero;

        KeyboardState currentKeyboardState;
        
        JoystickState currentJoystickState;

        public XnbManager Content { get; private set; }

        public AudioManager Audio { get; private set; }

        public MainGame()
        {
            // AudioManager を登録しなければ Audio 関連の機能は有効にならない仕様。
            Audio = new AudioManager(this);

            Content = new XnbManager(Services);
            Content.RootDirectory = "Content";

            graphics = new GraphicsManager(this);

            audioManager = new AudioComponent(this);
            Components.Add(audioManager);

            cat = new Cat();
            dog = new Dog();
        }

        protected override void LoadContent()
        {
            cat.Texture = Content.Load<Texture2D>("CatTexture");
            dog.Texture = Content.Load<Texture2D>("DogTexture");

            checkerTexture = Content.Load<Texture2D>("checker");

            quadDrawer = new QuadDrawer(Device);
        }

        protected override void Update(GameTime gameTime)
        {
            HandleInput();

            UpdateCamera();

            audioManager.Listener.Position = cameraPosition;
            audioManager.Listener.Forward = cameraForward;
            audioManager.Listener.Up = cameraUp;
            audioManager.Listener.Velocity = cameraVelocity;

            cat.Update(gameTime, audioManager);
            dog.Update(gameTime, audioManager);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            var context = Device.ImmediateContext;

            context.Clear(Color.CornflowerBlue);

            context.BlendState = BlendState.AlphaBlend;

            var view = Matrix.CreateLookAt(cameraPosition, cameraPosition + cameraForward, cameraUp);
            var projection = Matrix.CreatePerspectiveFieldOfView(1, context.Viewport.AspectRatio, 1, 100000);

            var groundTransform = Matrix.CreateScale(20000) * Matrix.CreateRotationX(MathHelper.PiOver2);

            quadDrawer.DrawQuad(context, checkerTexture, 32, groundTransform, view, projection);

            cat.Draw(context, quadDrawer, cameraPosition, view, projection);
            dog.Draw(context, quadDrawer, cameraPosition, view, projection);

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

        void UpdateCamera()
        {
            const float turnSpeed = 0.05f;
            const float accelerationSpeed = 4;
            const float frictionAmount = 0.98f;

            float turn = -currentJoystickState.ThumbSticks.Left.X * turnSpeed;

            if (currentKeyboardState.IsKeyDown(Keys.Left))
                turn += turnSpeed;

            if (currentKeyboardState.IsKeyDown(Keys.Right))
                turn -= turnSpeed;

            cameraForward = Vector3.TransformNormal(cameraForward, Matrix.CreateRotationY(turn));

            float accel = currentJoystickState.ThumbSticks.Left.Y * accelerationSpeed;

            if (currentKeyboardState.IsKeyDown(Keys.Up))
                accel += accelerationSpeed;

            if (currentKeyboardState.IsKeyDown(Keys.Down))
                accel -= accelerationSpeed;

            cameraVelocity += cameraForward * accel;

            cameraPosition += cameraVelocity;

            cameraVelocity *= frictionAmount;
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
