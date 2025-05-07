using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class SafeDictionary<TKey, TValue>
    {
        private enum OperationType { Add, Remove }
        
        private int InEach_;

        private readonly Dictionary<TKey, TValue> Values_;
        private readonly Queue<(OperationType type, KeyValuePair<TKey, TValue> item)> Ops_;
        
        public int Count => Values_.Count;
        public TValue this[TKey key] => Values_[key];
        
        public SafeDictionary()
        {
            InEach_ = 0;

            Values_ = new Dictionary<TKey, TValue>();
            Ops_ = new Queue<(OperationType type, KeyValuePair<TKey, TValue> item)>();
        }

        public void Add(TKey key, TValue value)
        {
            if (InEach_ > 0)
            {
                Ops_.Enqueue((OperationType.Add, new KeyValuePair<TKey, TValue>(key, value)));
            }
            else
            {
                Values_.Add(key, value);
            }
        }

        public void Remove(TKey key)
        {
            if (InEach_ > 0)
            {
                Ops_.Enqueue((OperationType.Remove, new KeyValuePair<TKey, TValue>(key, default)));
            }
            else
            {
                Values_.Remove(key);
            }
        }

        public void Clear()
        {
            Values_.Clear();
            Ops_.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return Values_.ContainsKey(key);
        }

        public void Foreach(Action<KeyValuePair<TKey, TValue>> func)
        {
            Flush();
            try
            {
                InEach_++;
                foreach (var item in Values_)
                {
                    func?.Invoke(item);
                }
            }
            finally
            {
                InEach_--;
            }
        }

        public void Foreach<P>(Action<KeyValuePair<TKey, TValue>, P> func, P param)
        {
            Flush();
            try
            {
                InEach_++;
                foreach (var item in Values_)
                {
                    func?.Invoke(item, param);
                }
            }
            finally
            {
                InEach_--;
            }
        }

        public void Foreach<P1, P2>(Action<KeyValuePair<TKey, TValue>, P1, P2> func, P1 param1, P2 param2)
        {
            Flush();
            try
            {
                InEach_++;
                foreach (var item in Values_)
                {
                    func?.Invoke(item, param1, param2);
                }
            }
            finally
            {
                InEach_--;
            }
        }
        
        public void Foreach<P1, P2, P3>(Action<KeyValuePair<TKey, TValue>, P1, P2, P3> func, P1 param1, P2 param2, P3 param3)
        {
            Flush();
            try
            {
                InEach_++;
                foreach (var item in Values_)
                {
                    func?.Invoke(item, param1, param2, param3);
                }
            }
            finally
            {
                InEach_--;
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
                InEach_++;
                foreach (var item in Values_)
                {
                    if (func?.Invoke(item) == true)
                    {
                        return item;
                    }
                }
            }
            finally
            {
                InEach_--;
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
                InEach_++;
                foreach (var item in Values_)
                {
                    if (func?.Invoke(item, param) == true)
                    {
                        return item;
                    }
                }
            }
            finally
            {
                InEach_--;
            }
            return default;
        }

        public void Flush()
        {
            if (InEach_ > 0)
            {
                return;
            }

            while (Ops_.Count > 0)
            {
                var (type, item) = Ops_.Dequeue();
                switch (type)
                {
                    case OperationType.Add:
                        Values_.Add(item.Key, item.Value);
                        break;
                    case OperationType.Remove:
                        Values_.Remove(item.Key);
                        break;
                }
            }
        }
    }
}