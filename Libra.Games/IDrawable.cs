#region Using

using System;

#endregion

namespace Libra.Games
{
    public interface IDrawable
    {
        event EventHandler DrawOrderChanged;

        event EventHandler VisibleChanged;

        int DrawOrder { get; }

        bool Visible { get; }

        void Draw(GameTime gameTime);
    }
}
