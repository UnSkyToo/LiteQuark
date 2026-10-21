using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// 稳定最大堆。比较结果相同时，先入队的元素先出队。<br/>
    /// 元素不可重复，入队期间相等性和哈希值必须保持不变。<br/>
    /// 修改影响排序的字段后，必须调用 UpdatePriority。
    /// </summary>
    public sealed class PriorityQueue<T>
    {
        private readonly List<Entry> _heap = new();
        private readonly Dictionary<T, int> _indices = new();
        private readonly Comparison<T> _comparer;
        private long _nextOrder;

        public int Count => _heap.Count;

        public PriorityQueue(Comparison<T> comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        public void Clear()
        {
            _heap.Clear();
            _indices.Clear();
            _nextOrder = 0;
        }

        public bool Contains(T item) => _indices.ContainsKey(item);

        public void Enqueue(T item)
        {
            var index = _heap.Count;

            _indices.Add(item, index);
            _heap.Add(new Entry(item, _nextOrder++));

            HeapifyUp(index);
        }

        public T Peek()
        {
            if (_heap.Count == 0)
            {
                throw new InvalidOperationException("Queue is empty.");
            }

            return _heap[0].Item;
        }

        public T Dequeue()
        {
            var item = Peek();
            RemoveAt(0);
            return item;
        }

        public bool Remove(T item)
        {
            if (!_indices.TryGetValue(item, out var index))
            {
                return false;
            }

            RemoveAt(index);
            return true;
        }

        public bool UpdatePriority(T item)
        {
            if (!_indices.TryGetValue(item, out var index))
            {
                return false;
            }

            RestoreHeap(index);
            return true;
        }

        private void RemoveAt(int index)
        {
            var lastIndex = _heap.Count - 1;
            _indices.Remove(_heap[index].Item);

            if (index == lastIndex)
            {
                _heap.RemoveAt(lastIndex);
                return;
            }

            var replacement = _heap[lastIndex];
            _heap[index] = replacement;
            _heap.RemoveAt(lastIndex);
            _indices[replacement.Item] = index;

            RestoreHeap(index);
        }

        private void RestoreHeap(int index)
        {
            if (index > 0 && Compare(_heap[index], _heap[(index - 1) / 2]) > 0)
            {
                HeapifyUp(index);
            }
            else
            {
                HeapifyDown(index);
            }
        }

        private int Compare(Entry x, Entry y)
        {
            var result  = _comparer(x.Item, y.Item);
            return result != 0 ? result : y.Order.CompareTo(x.Order);
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                var parent = (index - 1) / 2;
                if (Compare(_heap[index], _heap[parent]) <= 0)
                {
                    break;
                }
                Swap(index, parent);
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            while (true)
            {
                var left = index * 2 + 1;
                if (left >= _heap.Count)
                {
                    return;
                }

                var right = left + 1;
                var largest = left;

                if (right < _heap.Count && Compare(_heap[right], _heap[left]) > 0)
                {
                    largest = right;
                }

                if (Compare(_heap[largest], _heap[index]) <= 0)
                {
                    return;
                }

                Swap(index, largest);
                index = largest;
            }
        }

        private void Swap(int i, int j)
        {
            (_heap[i], _heap[j]) = (_heap[j], _heap[i]);

            _indices[_heap[i].Item] = i;
            _indices[_heap[j].Item] = j;
        }

        private readonly struct Entry
        {
            public readonly T Item;
            public readonly long Order;

            public Entry(T item, long order)
            {
                Item = item;
                Order = order;
            }
        }
    }
}
