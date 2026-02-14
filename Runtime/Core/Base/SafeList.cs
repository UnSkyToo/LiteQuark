using System;
using System.Collections;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class SafeList<T> : IEnumerable<T>
    {
        private enum OperationType { Add, Remove, Clear }
        
        private readonly Queue<(OperationType type, T item)> _ops;
        private readonly List<T> _values;
        private int _inEach;
        
        public int Count => _values.Count;
        public T this[int index] => _values[index];
        
        public SafeList()
        {
            _ops = new Queue<(OperationType type, T item)>();
            _values = new List<T>();
            _inEach = 0;
        }

        public void Add(T item)
        {
            if (_inEach > 0)
            {
                _ops.Enqueue((OperationType.Add, item));
            }
            else
            {
                _values.Add(item);
            }
        }

        public void Remove(T item)
        {
            if (_inEach > 0)
            {
                _ops.Enqueue((OperationType.Remove, item));
            }
            else
            {
                _values.Remove(item);
            }
        }

        public void Clear()
        {
            if (_inEach > 0)
            {
                _ops.Enqueue((OperationType.Clear, default));
            }
            else
            {
                _values.Clear();
                _ops.Clear();
            }
        }

        public bool Contains(T item)
        {
            return _values.Contains(item);
        }

        public void Foreach(Action<T> func)
        {
            Flush();
            try
            {
                _inEach++;
                foreach (var item in _values)
                {
                    func?.Invoke(item);
                }
            }
            finally
            {
                _inEach--;
            }
        }

        public void Foreach<P>(Action<T, P> func, P param)
        {
            Flush();
            try
            {
                _inEach++;
                foreach (var item in _values)
                {
                    func?.Invoke(item, param);
                }
            }
            finally
            {
                _inEach--;
            }
        }

        public void Foreach<P1, P2>(Action<T, P1, P2> func, P1 param1, P2 param2)
        {
            Flush();
            try
            {
                _inEach++;
                foreach (var item in _values)
                {
                    func?.Invoke(item, param1, param2);
                }
            }
            finally
            {
                _inEach--;
            }
        }
        
        public void Foreach<P1, P2, P3>(Action<T, P1, P2, P3> func, P1 param1, P2 param2, P3 param3)
        {
            Flush();
            try
            {
                _inEach++;
                foreach (var item in _values)
                {
                    func?.Invoke(item, param1, param2, param3);
                }
            }
            finally
            {
                _inEach--;
            }
        }
        
        /// <summary>
        /// Return T when func return true
        /// </summary>
        public T ForeachReturn(Func<T, bool> func)
        {
            Flush();
            try
            {
                _inEach++;
                foreach (var item in _values)
                {
                    if (func?.Invoke(item) == true)
                    {
                        return item;
                    }
                }
            }
            finally
            {
                _inEach--;
            }
            return default;
        }

        /// <summary>
        /// Return T when func return true
        /// </summary>
        public T ForeachReturn<P>(Func<T, P, bool> func, P param)
        {
            Flush();
            try
            {
                _inEach++;
                foreach (var item in _values)
                {
                    if (func?.Invoke(item, param) == true)
                    {
                        return item;
                    }
                }
            }
            finally
            {
                _inEach--;
            }
            return default;
        }
        
        public void Sort(IComparer<T> comparer)
        {
            Flush();
            _values.Sort(comparer);
        }

        public void Sort(Comparison<T> comparison)
        {
            Flush();
            _values.Sort(comparison);
        }

        public void Flush()
        {
            if (_inEach > 0)
            {
                return;
            }

            while (_ops.Count > 0)
            {
                var (type, item) = _ops.Dequeue();
                switch (type)
                {
                    case OperationType.Add:
                        _values.Add(item);
                        break;
                    case OperationType.Remove:
                        _values.Remove(item);
                        break;
                    case OperationType.Clear:
                        _values.Clear();
                        break;
                }
            }
        }

        // GC Alloc 40B
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            Flush();
            
            for (var index = 0; index < _values.Count; ++index)
            {
                yield return _values[index];
            }
        }

        // GC Alloc 40B
        IEnumerator IEnumerable.GetEnumerator()
        {
            Flush();
            
            for (var index = 0; index < _values.Count; ++index)
            {
                yield return _values[index];
            }
        }
    }
}