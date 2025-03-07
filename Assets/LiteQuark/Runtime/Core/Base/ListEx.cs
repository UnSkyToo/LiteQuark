using System;
using System.Collections;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class ListEx<T> : IEnumerable<T>
    {
        private bool Dirty_;
        private int InEach_;

        private readonly List<T> Values_;
        private readonly List<T> AddList_;
        private readonly List<T> RemoveList_;

        public int Count => AddList_.Count + Values_.Count - RemoveList_.Count;

        public T this[int index] => Values_[index];

        public ListEx()
        {
            Dirty_ = false;
            InEach_ = 0;

            Values_ = new List<T>();
            AddList_ = new List<T>();
            RemoveList_ = new List<T>();
        }

        public void Add(T item)
        {
            if (RemoveList_.Remove(item))
            {
                Dirty_ = true;
                return;
            }
            
            AddList_.Add(item);
            Dirty_ = true;
        }

        public void Remove(T item)
        {
            if (AddList_.Remove(item))
            {
                Dirty_ = true;
                return;
            }
            
            RemoveList_.Add(item);
            Dirty_ = true;
        }

        public void Clear()
        {
            RemoveList_.Clear();
            // AddList_.Clear();
            Values_.Clear();
            Dirty_ = false;
        }

        public bool Contains(T item)
        {
            return Values_.Contains(item) || AddList_.Contains(item);
        }

        public void Foreach(Action<T> func)
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

        public void Foreach<P>(Action<T, P> func, P param)
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

        public void Foreach<P1, P2>(Action<T, P1, P2> func, P1 param1, P2 param2)
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
        
        public void Foreach<P1, P2, P3>(Action<T, P1, P2, P3> func, P1 param1, P2 param2, P3 param3)
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
        public T ForeachReturn(Func<T, bool> func)
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

                foreach (var item in AddList_)
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
        public T ForeachReturn<P>(Func<T, P, bool> func, P param)
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
                
                foreach (var item in AddList_)
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
            if (!Dirty_ || InEach_ > 0)
            {
                return;
            }
            
            foreach (var item in RemoveList_)
            {
                Values_.Remove(item);
            }
            RemoveList_.Clear();
            
            foreach (var item in AddList_)
            {
                Values_.Add(item);
            }
            AddList_.Clear();
            
            Dirty_ = false;
        }

        // GC Alloc 40B
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            Flush();
            
            for (var index = 0; index < Values_.Count; ++index)
            {
                yield return Values_[index];
            }
        }

        // GC Alloc 40B
        IEnumerator IEnumerable.GetEnumerator()
        {
            Flush();
            
            for (var index = 0; index < Values_.Count; ++index)
            {
                yield return Values_[index];
            }
        }
    }
}