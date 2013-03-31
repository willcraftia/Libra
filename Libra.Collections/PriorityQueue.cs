#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Collections
{
    /// <summary>
    /// 優先度付き待ち行列です。
    /// この待ち行列の先頭は、指定された順序付けにおける最小の要素です。
    /// </summary>
    /// <remarks>
    /// このクラスの実装は、下記サイトを参考にしています。
    /// 
    /// http://ufcpp.net/study/algorithm/col_heap.html
    /// 
    /// なお、上記サイトでは、指定された順序付けにおける最大の要素を待ち行列の先頭にしています。
    /// </remarks>
    /// <typeparam name="T">キュー内の要素の型。</typeparam>
    public sealed class PriorityQueue<T> : IEnumerable<T>
    {
        /// <summary>
        /// 初期容量。
        /// List のデフォルト容量と等値。
        /// </summary>
        const int defaultCapacity = 4;

        /// <summary>
        /// ヒープ構造で要素を管理するためのリスト。
        /// </summary>
        List<T> heap;

        /// <summary>
        /// 比較オブジェクト。
        /// </summary>
        IComparer<T> comparer;

        /// <summary>
        /// 要素の数を取得します。
        /// </summary>
        public int Count
        {
            get { return heap.Count; }
        }

        /// <summary>
        /// インスタンスを生成します。
        /// 初期容量は 4、要素の比較には Comparer&lt;T&gt;.Default を用います
        /// </summary>
        public PriorityQueue()
            : this(defaultCapacity, null)
        {
        }

        /// <summary>
        /// インスタンスを生成します。
        /// 要素の比較には Comparer&lt;T&gt;.Default を用います
        /// </summary>
        /// <param name="capacity">初期容量。</param>
        public PriorityQueue(int capacity)
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
        public PriorityQueue(IComparer<T> comparer)
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
        public PriorityQueue(int capacity, IComparer<T> comparer)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException("capacity");

            heap = new List<T>(capacity);
            this.comparer = comparer ?? Comparer<T>.Default;
        }

        /// <summary>
        /// このクラスが返す列挙子は優先度に基づきません。
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return heap.GetEnumerator();
        }

        /// <summary>
        /// このクラスが返す列挙子は優先度に基づきません。
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 末尾に要素を追加します。
        /// </summary>
        /// <param name="item">要素。</param>
        public void Enqueue(T item)
        {
            var n = heap.Count;
            heap.Add(item);

            while (n != 0)
            {
                var i = (n - 1) / 2;
                if (Compare(n, i) < 0) Swap(n, i);

                n = i;
            }
        }

        /// <summary>
        /// 先頭の要素を削除してから取得します。
        /// </summary>
        /// <returns>要素。</returns>
        public T Dequeue()
        {
            T result = heap[0];

            var n = heap.Count - 1;
            heap[0] = heap[n];
            heap.RemoveAt(n);

            for (int i = 0, j; (j = 2 * i + 1) < n; )
            {
                if ((j != n - 1) && 0 < Compare(j, j + 1)) j++;

                if (0 < Compare(i, j)) Swap(j, i);

                i = j;
            }

            return result;
        }

        /// <summary>
        /// 先頭の要素を削除せずに取得します。
        /// </summary>
        /// <returns>要素。</returns>
        public T Peek()
        {
            return heap[0];
        }

        /// <summary>
        /// 全ての要素を削除します。
        /// </summary>
        public void Clear()
        {
            heap.Clear();
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
            return heap.Contains(item);
        }

        /// <summary>
        /// 指定の添字にある要素同士を比較します。
        /// </summary>
        /// <param name="index1">添字 1。</param>
        /// <param name="index2">添字 2。</param>
        /// <returns>
        /// -1 (添字 1 の要素が添字 2 の要素よりも小さい場合)、
        /// 0 (添字 1 の要素と添字 2 の要素が等しい場合)、
        /// 1 (添字 1 の要素が添字 2 の要素よりも大きい場合)。
        /// </returns>
        int Compare(int index1, int index2)
        {
            return comparer.Compare(heap[index1], heap[index2]);
        }

        /// <summary>
        /// 指定の添字にある要素同士を交換します。
        /// </summary>
        /// <param name="index1">添字 1。</param>
        /// <param name="index2">添字 2。</param>
        void Swap(int index1, int index2)
        {
            T temp = heap[index2];
            heap[index2] = heap[index1];
            heap[index1] = temp;
        }
    }
}
