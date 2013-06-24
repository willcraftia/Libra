#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics;
using Libra.Graphics.Toolkit;
using Libra.Input;

#endregion

namespace Samples.ShapeRendering
{
    public sealed class MainGame : Game
    {
        GraphicsManager graphicsManager;

        SpriteBatch spriteBatch;

        DebugShapeRenderer debugShapeRenderer;

        BoundingBox box;

        BoundingFrustum frustum;
        
        BoundingSphere sphere;

        public MainGame()
        {
            graphicsManager = new GraphicsManager(this);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(DeviceContext);

            debugShapeRenderer = new DebugShapeRenderer(DeviceContext);

            box = new BoundingBox(new Vector3(-3f), new Vector3(3f));

            Matrix frustumView = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitX, Vector3.Up);
            Matrix frustumProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 16f / 9f, 1f, 5f);
            frustum = new BoundingFrustum(frustumView * frustumProjection);

            sphere = new BoundingSphere(Vector3.Zero, 3f);
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            DeviceContext.Clear(Color.CornflowerBlue);

            float angle = (float) gameTime.TotalGameTime.TotalSeconds;
            Vector3 eye = new Vector3((float) Math.Cos(angle * .5f), 0f, (float) Math.Sin(angle * .5f)) * 12f;
            eye.Y = 5f;

            Matrix view = Matrix.CreateLookAt(eye, Vector3.Zero, Vector3.Up);
            Matrix projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, DeviceContext.Viewport.AspectRatio, 0.1f, 1000f);

            debugShapeRenderer.AddBoundingBox(box, Color.Yellow);
            debugShapeRenderer.AddBoundingFrustum(frustum, Color.Green);
            debugShapeRenderer.AddBoundingSphere(sphere, Color.Red);

            debugShapeRenderer.AddTriangle(new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 2f, 0f), Color.Purple);
            debugShapeRenderer.AddLine(new Vector3(0f, 0f, 0f), new Vector3(3f, 3f, 3f), Color.Brown);

            float elapsedTime = (float) gameTime.ElapsedGameTime.TotalSeconds;
            debugShapeRenderer.Draw(elapsedTime, view, projection);

            base.Draw(gameTime);
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
