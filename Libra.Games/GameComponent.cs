#region Using

using System;

#endregion

namespace Libra.Games
{
    public class GameComponent : IGameComponent, IUpdateable, IDisposable
    {
        public event EventHandler EnabledChanged;

        public event EventHandler UpdateOrderChanged;

        public event EventHandler Disposed;

        readonly Game game;

        bool enabled;
        
        int updateOrder;

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (enabled == value) return;

                enabled = value;
                OnEnabledChanged(this, EventArgs.Empty);
            }
        }

        public Game Game
        {
            get { return game; }
        }

        public int UpdateOrder
        {
            get { return updateOrder; }
            set
            {
                if (updateOrder == value) return;

                updateOrder = value;
                OnUpdateOrderChanged(this, EventArgs.Empty);
            }
        }

        protected GameComponent(Game game)
        {
            if (game == null) throw new ArgumentNullException("game");

            this.game = game;
            enabled = true;
        }

        public virtual void Initialize() { }

        public virtual void Update(GameTime gameTime) { }

        protected virtual void OnEnabledChanged(object sender, EventArgs e)
        {
            if (EnabledChanged != null)
                EnabledChanged(sender, e);
        }

        protected virtual void OnUpdateOrderChanged(object sender, EventArgs e)
        {
            if (UpdateOrderChanged != null)
                UpdateOrderChanged(sender, e);
        }

        protected virtual void DisposeOverride(bool disposing) { }

        #region IDisposable

        bool disposed;

        ~GameComponent()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed) return;

            DisposeOverride(disposing);

            if (Disposed != null)
                Disposed(this, EventArgs.Empty);

            disposed = true;
        }

        #endregion
    }
}
