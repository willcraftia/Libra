#region Using

using System;
using System.Collections.ObjectModel;

#endregion

namespace Libra.Games
{
    public sealed class GameComponentCollection : Collection<IGameComponent>
    {
        public event EventHandler<GameComponentCollectionEventArgs> ComponentAdded;

        public event EventHandler<GameComponentCollectionEventArgs> ComponentRemoved;

        public GameComponentCollection() { }

        protected override void ClearItems()
        {
            for (int i = 0; i < Count; i++)
                OnComponentRemoved(new GameComponentCollectionEventArgs(this[i]));

            base.ClearItems();
        }

        protected override void InsertItem(int index, IGameComponent item)
        {
            base.InsertItem(index, item);

            OnComponentAdded(new GameComponentCollectionEventArgs(item));
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            base.RemoveItem(index);
            
            OnComponentRemoved(new GameComponentCollectionEventArgs(item));
        }

        protected override void SetItem(int index, IGameComponent item)
        {
            var oldItem = this[index];

            OnComponentRemoved(new GameComponentCollectionEventArgs(oldItem));

            base.SetItem(index, item);
            
            OnComponentAdded(new GameComponentCollectionEventArgs(item));
        }

        void OnComponentAdded(GameComponentCollectionEventArgs e)
        {
            if (ComponentAdded != null)
                ComponentAdded(this, e);
        }

        void OnComponentRemoved(GameComponentCollectionEventArgs e)
        {
            if (ComponentRemoved != null)
                ComponentRemoved(this, e);
        }
    }
}
