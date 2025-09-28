using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// 一个最大堆优先队列，优先级大的元素先出队
    /// </summary>
    public class PriorityQueue<T>
    {
        private readonly List<T> _heap;
        private readonly Comparison<T> _comparer;

        public int Count => _heap.Count;

        public PriorityQueue(Comparison<T> comparer)
        {
            _heap = new List<T>();
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        public void Clear()
        {
            _heap.Clear();
        }

        public void Enqueue(T item)
        {
            _heap.Add(item);
            HeapifyUp(_heap.Count - 1);
        }

        public T Dequeue()
        {
            if (_heap.Count == 0)
            {
                throw new InvalidOperationException("Queue is empty.");
            }

            var root = _heap[0];
            var lastIndex = _heap.Count - 1;
            _heap[0] = _heap[lastIndex];
            _heap.RemoveAt(lastIndex);

            if (_heap.Count > 0)
            {
                HeapifyDown(0);
            }

            return root;
        }

        public T Peek()
        {
            if (_heap.Count == 0)
            {
                throw new InvalidOperationException("Queue is empty.");
            }

            return _heap[0];
        }

        public bool Remove(T item)
        {
            var index = _heap.IndexOf(item);
            if (index < 0)
            {
                return false;
            }
            
            var lastIndex = _heap.Count - 1;
            _heap[index] = _heap[lastIndex];
            _heap.RemoveAt(lastIndex);

            if (index < _heap.Count)
            {
                HeapifyUp(index);
                HeapifyDown(index);
            }
            
            return true;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                var parent = (index - 1) / 2;
                if (_comparer(_heap[index], _heap[parent]) <= 0)
                {
                    break;
                }
                Swap(index, parent);
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            var lastIndex = _heap.Count - 1;
            while (true)
            {
                var left = index * 2 + 1;
                var right = left + 1;
                var largest = index;

                if (left <= lastIndex && _comparer(_heap[left], _heap[largest]) > 0)
                {
                    largest = left;
                }

                if (right <= lastIndex && _comparer(_heap[right], _heap[largest]) > 0)
                {
                    largest = right;
                }

                if (largest == index)
                {
                    break;
                }

                Swap(index, largest);
                index = largest;
            }
        }

        private void Swap(int i, int j)
        {
            (_heap[i], _heap[j]) = (_heap[j], _heap[i]);
        }
    }
}