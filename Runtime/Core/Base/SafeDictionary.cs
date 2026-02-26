using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class SafeDictionary<TKey, TValue>
    {
        private enum OperationType { Add, Remove, Clear }
        
        private readonly Queue<(OperationType type, KeyValuePair<TKey, TValue> item)> _ops;
        private readonly Dictionary<TKey, TValue> _values;
        private int _inEach;
        
        public int Count => _values.Count;
        public TValue this[TKey key] => _values[key];
        
        public SafeDictionary()
        {
            _ops = new Queue<(OperationType type, KeyValuePair<TKey, TValue> item)>();
            _values = new Dictionary<TKey, TValue>();
            _inEach = 0;
        }

        public void Add(TKey key, TValue value)
        {
            if (_inEach > 0)
            {
                _ops.Enqueue((OperationType.Add, new KeyValuePair<TKey, TValue>(key, value)));
            }
            else
            {
                _values.Add(key, value);
            }
        }

        public void Remove(TKey key)
        {
            if (_inEach > 0)
            {
                _ops.Enqueue((OperationType.Remove, new KeyValuePair<TKey, TValue>(key, default)));
            }
            else
            {
                _values.Remove(key);
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

        public bool ContainsKey(TKey key)
        {
            return _values.ContainsKey(key);
        }

        public void Foreach(Action<KeyValuePair<TKey, TValue>> func)
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

        public void Foreach<P>(Action<KeyValuePair<TKey, TValue>, P> func, P param)
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

        public void Foreach<P1, P2>(Action<KeyValuePair<TKey, TValue>, P1, P2> func, P1 param1, P2 param2)
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
        
        public void Foreach<P1, P2, P3>(Action<KeyValuePair<TKey, TValue>, P1, P2, P3> func, P1 param1, P2 param2, P3 param3)
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
        public KeyValuePair<TKey, TValue> ForeachReturn(Func<KeyValuePair<TKey, TValue>, bool> func)
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
        public KeyValuePair<TKey, TValue> ForeachReturn<P>(Func<KeyValuePair<TKey, TValue>, P, bool> func, P param)
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
                        _values.Add(item.Key, item.Value);
                        break;
                    case OperationType.Remove:
                        _values.Remove(item.Key);
                        break;
                    case OperationType.Clear:
                        _values.Clear();
                        break;
                }
            }
        }
    }
}