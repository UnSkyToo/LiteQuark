﻿using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class SafeDictionary<TKey, TValue>
    {
        private enum OperationType { Add, Remove }
        
        private int _inEach;

        private readonly Dictionary<TKey, TValue> _values;
        private readonly Queue<(OperationType type, KeyValuePair<TKey, TValue> item)> _ops;
        
        public int Count => _values.Count;
        public TValue this[TKey key] => _values[key];
        
        public SafeDictionary()
        {
            _inEach = 0;

            _values = new Dictionary<TKey, TValue>();
            _ops = new Queue<(OperationType type, KeyValuePair<TKey, TValue> item)>();
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
            _values.Clear();
            _ops.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return _values.ContainsKey(key);
        }

        public void Foreach(Action<KeyValuePair<TKey, TValue>> func)
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

        public void Foreach<P>(Action<KeyValuePair<TKey, TValue>, P> func, P param)
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

        public void Foreach<P1, P2>(Action<KeyValuePair<TKey, TValue>, P1, P2> func, P1 param1, P2 param2)
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
        
        public void Foreach<P1, P2, P3>(Action<KeyValuePair<TKey, TValue>, P1, P2, P3> func, P1 param1, P2 param2, P3 param3)
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
        public KeyValuePair<TKey, TValue> ForeachReturn(Func<KeyValuePair<TKey, TValue>, bool> func)
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
        public KeyValuePair<TKey, TValue> ForeachReturn<P>(Func<KeyValuePair<TKey, TValue>, P, bool> func, P param)
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
                }
            }
        }
    }
}