#region Using

using System;
using Libra.Graphics;

#endregion

namespace Libra.Games
{
    public class DrawableGameComponent : GameComponent, IDrawable
    {
        public event EventHandler DrawOrderChanged;

        public event EventHandler VisibleChanged;

        int drawOrder;

        bool visible;

        public int DrawOrder
        {
            get { return drawOrder; }
            set
            {
                if (drawOrder == value) return;

                drawOrder = value;

                OnDrawOrderChanged(this, EventArgs.Empty);
            }
        }

        public bool Visible
        {
            get { return visible; }
            set
            {
                if (visible == value) return;

                visible = value;

                OnVisibleChanged(this, EventArgs.Empty);
            }
        }

        public Device Device
        {
            get { return Game.Device; }
        }

        protected DrawableGameComponent(Game game)
            : base(game)
        {
            visible = true;
        }

        public override void Initialize()
        {
            base.Initialize();

            LoadContent();
        }

        public virtual void Draw(GameTime gameTime) { }

        protected virtual void LoadContent() { }

        protected virtual void UnloadContent() { }

        protected virtual void OnDrawOrderChanged(object sender, EventArgs e)
        {
            if (DrawOrderChanged != null)
                DrawOrderChanged(sender, e);
        }

        protected virtual void OnVisibleChanged(object sender, EventArgs e)
        {
            if (VisibleChanged != null)
                VisibleChanged(sender, e);
        }

        protected override void DisposeOverride(bool disposing)
        {
            if (disposing)
            {
                UnloadContent();
            }

            base.DisposeOverride(disposing);
        }
    }
}
