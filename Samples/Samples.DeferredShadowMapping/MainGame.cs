#region Using

using System;
using Libra.Games;

#endregion

namespace Samples.DeferredShadowMapping
{
    public sealed class MainGame : Game
    {
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
