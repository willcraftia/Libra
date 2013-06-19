#region Using

using System;

#endregion

namespace Libra.Collections
{
    public sealed class ConcurrentPool<T> where T : class
    {
        Pool<T> pool;

        public int Count
        {
            get
            {
                lock (this)
                {
                    return pool.Count;
                }
            }
        }

        public int InitialCapacity
        {
            get { return pool.InitialCapacity; }
        }

        public int MaxCapacity
        {
            get
            {
                lock (this)
                {
                    return pool.MaxCapacity;
                }
            }
            set
            {
                lock (this)
                {
                    pool.MaxCapacity = value;
                }
            }
        }

        public int TotalObjectCount
        {
            get
            {
                lock (this)
                {
                    return pool.TotalObjectCount;
                }
            }
        }

        public ConcurrentPool(Func<T> createFunction)
        {
            pool = new Pool<T>(createFunction);
        }

        public void Prepare(int initialCapacity)
        {
            pool.Prepare(initialCapacity);
        }

        public T Borrow()
        {
            lock (this)
            {
                return pool.Borrow();
            }
        }

        public bool TryBorrow(out T result)
        {
            lock (this)
            {
                return pool.TryBorrow(out result);
            }
        }

        public void Return(T obj)
        {
            lock (this)
            {
                pool.Return(obj);
            }
        }

        public void Clear()
        {
            lock (this)
            {
                pool.Clear();
            }
        }
    }
}
