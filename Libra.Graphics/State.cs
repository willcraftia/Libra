#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public abstract class State
    {
        bool frozen;

        string name;

        public string Name
        {
            get { return name; }
            set
            {
                AssertNotFrozen();
                name = value;
            }
        }

        protected State() { }

        public void Freeze()
        {
            frozen = true;
        }

        internal void AssertNotFrozen()
        {
            if (frozen) throw new InvalidOperationException("Instance frozen.");
        }
    }
}
