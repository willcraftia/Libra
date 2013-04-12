#region Using

using System;
using Libra.Games;
using Libra.Xnb;

#endregion

namespace Samples.Audio3D
{
    public sealed class MainGame : Game
    {
        GraphicsManager graphics;

        public XnbManager Content { get; private set; }

        public MainGame()
        {
            Content = new XnbManager(Services);
            Content.RootDirectory = "Content";

            graphics = new GraphicsManager(this);
        }

        protected override void LoadContent()
        {
            var soundEffect = Content.Load<Libra.Audio.SoundEffect>("DogSound");

            base.LoadContent();
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
