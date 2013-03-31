#region Using

using System;
using System.Windows.Forms;
using Libra.Graphics;
using Libra.Input;
using Libra.Input.Forms;

#endregion

namespace Libra.Games.Forms
{
    public abstract class FormGamePlatform : IGamePlatform, IDisposable
    {
        #region FormGameWindow

        sealed class FormGameWindow : GameWindow
        {
            bool allowUserResizing;

            public override bool AllowUserResizing
            {
                get { return allowUserResizing; }
                set
                {
                    if (allowUserResizing == value)
                        return;

                    allowUserResizing = value;

                    if (allowUserResizing)
                    {
                        Form.FormBorderStyle = FormBorderStyle.Sizable;
                    }
                    else
                    {
                        Form.FormBorderStyle = FormBorderStyle.FixedSingle;
                    }
                }
            }

            public override Rectangle ClientBounds
            {
                get
                {
                    var size = Form.ClientSize;
                    return new Rectangle(0, 0, size.Width, size.Height);
                }
            }

            public override IntPtr Handle
            {
                get { return Form.Handle; }
            }

            internal Form Form { get; private set; }

            internal FormGameWindow(Form form)
            {
                Form = form;
                Form.ClientSizeChanged += OnClientSizeChanged;

                // デフォルトの振る舞いとしてフォームをサイズ変更不可に初期化。
                Form.FormBorderStyle = FormBorderStyle.FixedSingle;

                Title = form.Text;
            }

            protected override void SetTitle(string title)
            {
                Form.Text = Title;
            }

            void OnClientSizeChanged(object sender, EventArgs e)
            {
                OnClientSizeChanged();
            }
        }

        #endregion

        public event EventHandler Activated;

        public event EventHandler Deactivated;

        public event EventHandler Exiting;

        Game game;

        MessageFilter messageFilter;

        public GameWindow Window { get; private set; }

        public IGraphicsFactory GraphicsFactory { get; private set; }
        
        public Form Form { get; private set; }

        public bool DirectInputEnabled { get; set; }

        public FormGamePlatform(Game game, Form form)
        {
            if (game == null) throw new ArgumentNullException("game");
            if (form == null) throw new ArgumentNullException("form");

            this.game = game;
            Form = form;
            Form.Activated += OnActivated;
            Form.Deactivate += OnDeactivated;
            Form.FormClosing += OnClosing;

            // TODO
            //
            // Game インスタンスと共に初期化されるオブジェクトを
            // サービス登録することはおかしい。

            game.Services.AddService<IGamePlatform>(this);
        }

        public virtual void Initialize()
        {
            if (Window != null)
                throw new InvalidOperationException("GameWindow already exists.");

            Window = new FormGameWindow(Form);
            GraphicsFactory = CreateGraphicsFactory();

            messageFilter = new MessageFilter(Window.Handle);
            Application.AddMessageFilter(messageFilter);

            if (!Keyboard.Initialized) Keyboard.Initialize(CreateKeyboard());
            if (!Mouse.Initialized) Mouse.Initialize(CreateMouse());
            if (!Joystick.Initialized) Joystick.Initialize(CreateJoystick());
        }

        protected abstract IGraphicsFactory CreateGraphicsFactory();

        protected abstract IKeyboard CreateKeyboard();

        protected abstract IMouse CreateMouse();

        protected abstract IJoystick CreateJoystick();

        public abstract void Run(TickCallback tick);

        public virtual void Exit()
        {
            Form.Close();
        }

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

        protected virtual void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (Exiting != null)
                Exiting(this, EventArgs.Empty);
        }

        #region IDisposable

        protected bool IsDisposed { get; private set; }

        ~FormGamePlatform()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeOverride(bool disposing) { }

        void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
                if (messageFilter != null)
                    Application.RemoveMessageFilter(messageFilter);
            }

            DisposeOverride(disposing);

            IsDisposed = true;
        }

        #endregion
    }
}
