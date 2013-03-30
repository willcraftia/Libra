#region Using

using System;

#endregion

namespace Libra.Games
{
    public interface IUpdateable
    {
        event EventHandler EnabledChanged;

        event EventHandler UpdateOrderChanged;

        bool Enabled { get; }

        int UpdateOrder { get; }

        void Update(GameTime gameTime);
    }
}
