#region Using

using System;

#endregion

namespace Libra.Games
{
    public sealed class GameComponentCollectionEventArgs : EventArgs
    {
        public IGameComponent GameComponent { get; private set; }

        public GameComponentCollectionEventArgs(IGameComponent gameComponent)
        {
            if (gameComponent == null) throw new ArgumentNullException("gameComponent");

            GameComponent = gameComponent;
        }
    }
}
