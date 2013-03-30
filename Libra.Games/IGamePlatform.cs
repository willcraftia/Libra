#region Using

using System;
using Libra.Graphics;
using Libra.Input;

#endregion

namespace Libra.Games
{
    public interface IGamePlatform
    {
        event EventHandler Activated;

        event EventHandler Deactivated;

        event EventHandler Exiting;

        GameWindow Window { get; }

        IGameTimer GameTimer { get; }

        IGraphicsFactory GraphicsFactory { get; }

        void Initialize();

        void Run(TickCallback tick);

        void Exit();

        IKeyboard CreateKeyboard();

        IMouse CreateMouse();

        IJoystick CreateJoystick();
    }
}
