#region Using

using System;

#endregion

namespace Libra.Games
{
    public abstract class GameWindow
    {
        public event EventHandler ClientSizeChanged;
        
        string title = string.Empty;

        public abstract bool AllowUserResizing { get; set; }

        public abstract Rectangle ClientBounds { get; }

        public abstract IntPtr Handle { get; }

        public string Title
        {
            get { return title; }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                
                if (title == value) return;

                title = value;

                SetTitle(title);
            }
        }

        protected GameWindow() { }

        protected void OnClientSizeChanged()
        {
            if (ClientSizeChanged != null)
                ClientSizeChanged(this, EventArgs.Empty);
        }

        protected abstract void SetTitle(string title);
    }
}
