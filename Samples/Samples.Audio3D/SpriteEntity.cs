#region Using

using System;
using Libra;
using Libra.Games;
using Libra.Graphics;

#endregion

namespace Samples.Audio3D
{
    public abstract class SpriteEntity : IAudioEmitter
    {
        Vector3 position;

        Vector3 forward;

        Vector3 up;

        Vector3 velocity;

        Texture2D texture;

        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        public Vector3 Forward
        {
            get { return forward; }
            set { forward = value; }
        }

        public Vector3 Up
        {
            get { return up; }
            set { up = value; }
        }

        public Vector3 Velocity
        {
            get { return velocity; }
            protected set { velocity = value; }
        }

        public Texture2D Texture
        {
            get { return texture; }
            set { texture = value; }
        }

        public abstract void Update(GameTime gameTime, AudioComponent audioManager);

        public void Draw(QuadDrawer quadDrawer, Vector3 cameraPosition, Matrix view, Matrix projection)
        {
            var world = Matrix.CreateTranslation(0, 1, 0) *
                        Matrix.CreateScale(800) *
                        Matrix.CreateConstrainedBillboard(Position, cameraPosition, Up, null, null);

            quadDrawer.DrawQuad(Texture, 1, world, view, projection);
        }
    }
}
