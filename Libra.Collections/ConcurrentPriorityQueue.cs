#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Collections
{
    /// <summary>
    /// スレッド セーフな優先度付き待ち行列です。
    /// IEnumerable&lt;T&gt; の実装は面倒なため実装しません。
    /// </summary>
    /// <typeparam name="T">要素の型。</typeparam>
    public sealed class ConcurrentPriorityQueue<T>
    {
        /// <summary>
        /// 初期容量。
        /// List のデフォルト容量と等値。
        /// </summary>
        const int defaultCapacity = 4;

        /// <summary>
        /// 優先度付き待ち行列。
        /// </summary>
        PriorityQueue<T> queue;

        /// <summary>
        /// 要素の数を取得します。
        /// </summary>
        public int Count
        {
            get
            {
                lock (queue)
                {
                    return queue.Count;
                }
            }
        }

        /// <summary>
        /// インスタンスを生成します。
        /// 初期容量は 4、要素の比較には Comparer&lt;T&gt;.Default を用います
        /// </summary>
        public ConcurrentPriorityQueue()
            : this(defaultCapacity, null)
        {
        }

        /// <summary>
        /// インスタンスを生成します。
        /// 要素の比較には Comparer&lt;T&gt;.Default を用います
        /// </summary>
        /// <param name="capacity">初期容量。</param>
        public ConcurrentPriorityQueue(int capacity)
            : this(capacity, null)
        {
        }

        /// <summary>
        /// インスタンスを生成します。
        /// 初期容量は 4 を用います
        /// </summary>
        /// <param name="comparer">
        /// 要素の比較オブジェクト。
        /// null を指定した場合は Comparer&lt;T&gt;.Default を用います
        /// </param>
        public ConcurrentPriorityQueue(IComparer<T> comparer)
            : this(defaultCapacity, comparer)
        {
        }

        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="capacity">初期容量。</param>
        /// <param name="comparer">
        /// 要素の比較オブジェクト。
        /// null を指定した場合は Comparer&lt;T&gt;.Default を用います
        /// </param>
        public ConcurrentPriorityQueue(int capacity, IComparer<T> comparer)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException("capacity");

            queue = new PriorityQueue<T>(capacity, comparer);
        }

        /// <summary>
        /// 末尾に要素を追加します。
        /// </summary>
        /// <param name="item">要素。</param>
        public void Enqueue(T item)
        {
            lock (queue)
            {
                queue.Enqueue(item);
            }
        }

        /// <summary>
        /// 先頭の要素を削除してから取得することを試行します。
        /// </summary>
        /// <param name="result">
        /// 先頭の要素、あるいは、非同期に待ち行列が空になっていた場合は型 T のデフォルト。
        /// </param>
        /// <returns>
        /// true (先頭の要素を取得できた場合)、false (それ以外の場合)。
        /// </returns>
        public bool TryDequeue(out T result)
        {
            lock (queue)
            {
                if (queue.Count == 0)
                {
                    result = default(T);
                    return false;
                }
                else
                {
                    result = queue.Dequeue();
                    return true;
                }
            }
        }

        /// <summary>
        /// 先頭の要素を削除せずに取得することを試行します。
        /// </summary>
        /// <param name="result">
        /// 先頭の要素、あるいは、非同期に待ち行列が空になっていた場合は型 T のデフォルト。
        /// </param>
        /// <returns>
        /// true (先頭の要素を取得できた場合)、false (それ以外の場合)。
        /// </returns>
        public bool TryPeek(out T result)
        {
            lock (queue)
            {
                if (queue.Count == 0)
                {
                    result = default(T);
                    return false;
                }
                else
                {
                    result = queue.Peek();
                    return true;
                }
            }
        }

        /// <summary>
        /// 全ての要素を削除します。
        /// </summary>
        public void Clear()
        {
            lock (queue)
            {
                queue.Clear();
            }
        }

        /// <summary>
        /// 指定の要素が存在するか否かを検査します。
        /// </summary>
        /// <param name="item">要素。</param>
        /// <returns>
        /// true (要素が存在する場合)、false (それ以外の場合)。
        /// </returns>
        public bool Contains(T item)
        {
            lock (queue)
            {
                return queue.Contains(item);
            }
        }
    }
}
