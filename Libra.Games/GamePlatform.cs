#region Using

using System;
using System.Configuration;
using Libra.Graphics;
using Libra.Input;

#endregion

namespace Libra.Games
{
    public abstract class GamePlatform
    {
        public const string AppSettingKey = "Libra.Games.GamePlatform";

        public const string DefaultImplementation = "Libra.Games.Forms.SharpDX.SdxFormGamePlatform, Libra.Games.Forms.SharpDX, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        public event EventHandler Activated;

        public event EventHandler Deactivated;

        public event EventHandler Exiting;

        bool isMouseVisible;

        protected internal bool IsMouseVisible
        {
            get { return isMouseVisible; }
            set
            {
                if (isMouseVisible == value)
                    return;

                isMouseVisible = value;

                OnIsMouseVisible();
            }
        }

        protected internal abstract GameWindow Window { get; }

        protected internal abstract IKeyboard Keyboard { get; }

        protected internal abstract IMouse Mouse { get; }

        protected internal abstract IJoystick Joystick { get; }

        internal static GamePlatform CreateGamePlatform()
        {
            // app.config 定義を参照。
            var implementation = ConfigurationManager.AppSettings[AppSettingKey];

            // app.config で未定義ならば SharpDX 実装をデフォルト指定。
            if (implementation == null)
                implementation = DefaultImplementation;

            var type = Type.GetType(implementation);
            return Activator.CreateInstance(type) as GamePlatform;
        }

        protected internal abstract void Initialize();

        protected internal abstract void Run(TickCallback tick);

        protected internal abstract void Exit();

        protected virtual void OnActivated(object sender, EventArgs e)
        {
            if (Activated != null)
                Activated(this, EventArgs.Empty);
        }

        protected virtual void OnDeactivated(object sender, EventArgs e)
        {
            if (Deactivated != null)
                Deactivated(this, EventArgs.Empty);
        }

        protected virtual void OnExiting(object sender, EventArgs e)
        {
            if (Exiting != null)
                Exiting(this, EventArgs.Empty);
        }

        protected virtual void OnIsMouseVisible() { }
    }
}
