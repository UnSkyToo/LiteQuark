using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class DictionaryEx<TKey, TValue>
    {
        private bool Dirty_;
        private int InEach_;

        private readonly Dictionary<TKey, TValue> Values_;
        private readonly Dictionary<TKey, TValue> AddList_;
        private readonly List<TKey> RemoveList_;

        public int Count => AddList_.Count + Values_.Count - RemoveList_.Count;

        public TValue this[TKey key]
        {
            get
            {
                if (Values_.ContainsKey(key))
                {
                    return Values_[key];
                }

                if (AddList_.ContainsKey(key))
                {
                    return AddList_[key];
                }

                return default;
            }
        }

        public DictionaryEx()
        {
            Dirty_ = false;
            InEach_ = 0;

            Values_ = new Dictionary<TKey, TValue>();
            AddList_ = new Dictionary<TKey, TValue>();
            RemoveList_ = new List<TKey>();
        }

        public void Add(TKey key, TValue value)
        {
            AddList_.Add(key, value);
            Dirty_ = true;
        }

        public void Remove(TKey key)
        {
            RemoveList_.Add(key);
            Dirty_ = true;
        }

        public void Clear()
        {
            RemoveList_.Clear();
            AddList_.Clear();
            Values_.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return Values_.ContainsKey(key) || AddList_.ContainsKey(key);
        }

        public void Foreach(Action<TKey, TValue> func)
        {
            Flush();
            InEach_++;
            foreach (var chunk in Values_)
            {
                func?.Invoke(chunk.Key, chunk.Value);
            }
            InEach_--;
        }

        public void Foreach<P>(Action<TKey, TValue, P> func, P param)
        {
            Flush();
            InEach_++;
            foreach (var chunk in Values_)
            {
                func?.Invoke(chunk.Key, chunk.Value, param);
            }
            InEach_--;
        }
        
        public void Foreach<P1, P2>(Action<TKey, TValue, P1, P2> func, P1 param1, P2 param2)
        {
            Flush();
            InEach_++;
            foreach (var chunk in Values_)
            {
                func?.Invoke(chunk.Key, chunk.Value, param1, param2);
            }
            InEach_--;
        }
        
        public void Foreach<P1, P2, P3>(Action<TKey, TValue, P1, P2, P3> func, P1 param1, P2 param2, P3 param3)
        {
            Flush();
            InEach_++;
            foreach (var chunk in Values_)
            {
                func?.Invoke(chunk.Key, chunk.Value, param1, param2, param3);
            }
            InEach_--;
        }
        
        /// <summary>
        /// Return T when func return true
        /// </summary>
        public TValue ForeachReturn(Func<TKey, TValue, bool> func)
        {
            Flush();
            InEach_++;
            foreach (var chunk in Values_)
            {
                if (func?.Invoke(chunk.Key, chunk.Value) == true)
                {
                    return chunk.Value;
                }
            }
            InEach_--;
            return default;
        }

        /// <summary>
        /// Return T when func return true
        /// </summary>
        public TValue ForeachReturn<P>(Func<TKey, TValue, P, bool> func, P param)
        {
            Flush();
            InEach_++;
            foreach (var chunk in Values_)
            {
                if (func?.Invoke(chunk.Key, chunk.Value, param) == true)
                {
                    return chunk.Value;
                }
            }
            InEach_--;
            return default;
        }

        public void Flush()
        {
            if (Dirty_ && InEach_ == 0)
            {
                foreach (var key in RemoveList_)
                {
                    Values_.Remove(key);
                }

                RemoveList_.Clear();

                foreach (var chunk in AddList_)
                {
                    Values_.Add(chunk.Key, chunk.Value);
                }

                AddList_.Clear();
            }
        }
    }
}