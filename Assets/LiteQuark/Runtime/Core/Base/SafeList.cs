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
                _ops.Clear();
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
            _inEach++;
            try
            {
                foreach (var item in _values)
                {
                    func?.Invoke(item);
                }
            }
            finally
            {
                _inEach--;
                Flush();
            }
        }

        public void Foreach<P>(Action<T, P> func, P param)
        {
            Flush();
            _inEach++;
            try
            {
                foreach (var item in _values)
                {
                    func?.Invoke(item, param);
                }
            }
            finally
            {
                _inEach--;
                Flush();
            }
        }

        public void Foreach<P1, P2>(Action<T, P1, P2> func, P1 param1, P2 param2)
        {
            Flush();
            _inEach++;
            try
            {
                foreach (var item in _values)
                {
                    func?.Invoke(item, param1, param2);
                }
            }
            finally
            {
                _inEach--;
                Flush();
            }
        }

        public void Foreach<P1, P2, P3>(Action<T, P1, P2, P3> func, P1 param1, P2 param2, P3 param3)
        {
            Flush();
            _inEach++;
            try
            {
                foreach (var item in _values)
                {
                    func?.Invoke(item, param1, param2, param3);
                }
            }
            finally
            {
                _inEach--;
                Flush();
            }
        }

        /// <summary>
        /// Return T when func return true
        /// </summary>
        public T ForeachReturn(Func<T, bool> func)
        {
            Flush();
            _inEach++;
            try
            {
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
                Flush();
            }
            return default;
        }

        /// <summary>
        /// Return T when func return true
        /// </summary>
        public T ForeachReturn<P>(Func<T, P, bool> func, P param)
        {
            Flush();
            _inEach++;
            try
            {
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
                Flush();
            }
            return default;
        }

        public void Sort(IComparer<T> comparer)
        {
            if (_inEach > 0)
            {
                throw new InvalidOperationException("Cannot sort SafeList during iteration.");
            }
            Flush();
            _values.Sort(comparer);
        }

        public void Sort(Comparison<T> comparison)
        {
            if (_inEach > 0)
            {
                throw new InvalidOperationException("Cannot sort SafeList during iteration.");
            }
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

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 优先通过 foreach 使用。<br/>
        /// 手动使用时必须 Dispose，且禁止复制已启动的枚举器。
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            public T Current { get; private set; }

            object IEnumerator.Current => Current;

            private SafeList<T> _owner;
            private int _index;
            private bool _started;

            internal Enumerator(SafeList<T> owner)
            {
                _owner = owner;
                _index = 0;
                _started = false;
                Current = default;
            }

            public bool MoveNext()
            {
                if (_owner == null)
                {
                    return false;
                }

                if (!_started)
                {
                    _owner.Flush();
                    _owner._inEach++;
                    _started = true;
                }

                if (_index < _owner._values.Count)
                {
                    Current = _owner._values[_index++];
                    return true;
                }

                Dispose();
                return false;
            }

            public void Dispose()
            {
                var owner = _owner;
                var started = _started;

                _owner = null;
                _started = false;
                Current = default;

                if (started)
                {
                    owner._inEach--;
                    owner.Flush();
                }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }
        }
    }
}
