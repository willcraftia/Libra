#region Using

using System;
using System.Windows.Forms;
using Libra.Graphics;
using Libra.Input;
using Libra.Input.Forms;

#endregion

namespace Libra.Games.Forms
{
    public abstract class FormGamePlatform : GamePlatform, IDisposable
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

        #region NullJoystick

        sealed class NullJoystick : IJoystick
        {
            public JoystickState GetState()
            {
                return new JoystickState();
            }
        }

        #endregion

        GameWindow window;

        MessageFilter messageFilter;

        FormKeyboard keyboard;

        FormMouse mouse;

        NullJoystick joystick;

        protected Form Form { get; private set; }

        protected override GameWindow Window
        {
            get { return window; }
        }

        protected override IKeyboard Keyboard
        {
            get { return keyboard; }
        }

        protected override IMouse Mouse
        {
            get { return mouse; }
        }

        protected override IJoystick Joystick
        {
            get { return joystick; }
        }

        protected FormGamePlatform() { }

        protected override void Initialize()
        {
            Form = CreateForm();
            Form.Activated += OnActivated;
            Form.Deactivate += OnDeactivated;
            Form.FormClosing += OnClosing;

            Cursor.Hide();

            window = new FormGameWindow(Form);

            keyboard = new FormKeyboard();
            mouse = new FormMouse(Form);
            joystick = new NullJoystick();

            messageFilter = new MessageFilter(window.Handle, keyboard, mouse);
            Application.AddMessageFilter(messageFilter);
        }

        protected override void Exit()
        {
            Form.Close();
        }

        protected override void OnIsMouseVisible()
        {
            if (IsMouseVisible)
            {
                Cursor.Show();
            }
            else
            {
                Cursor.Hide();
            }

            base.OnIsMouseVisible();
        }

        protected virtual Form CreateForm()
        {
            return new Form();
        }

        protected virtual void OnClosing(object sender, FormClosingEventArgs e)
        {
            OnExiting(this, EventArgs.Empty);
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
