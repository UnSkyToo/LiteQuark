using System;
using System.Collections;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class SafeList<T> : IEnumerable<T>
    {
        private enum OperationType { Add, Remove }
        
        private int InEach_;

        private readonly List<T> Values_;
        private readonly Queue<(OperationType type, T item)> Ops_;
        
        public int Count => Values_.Count;
        public T this[int index] => Values_[index];
        
        public SafeList()
        {
            InEach_ = 0;

            Values_ = new List<T>();
            Ops_ = new Queue<(OperationType type, T item)>();
        }

        public void Add(T item)
        {
            if (InEach_ > 0)
            {
                Ops_.Enqueue((OperationType.Add, item));
            }
            else
            {
                Values_.Add(item);
            }
        }

        public void Remove(T item)
        {
            if (InEach_ > 0)
            {
                Ops_.Enqueue((OperationType.Remove, item));
            }
            else
            {
                Values_.Remove(item);
            }
        }

        public void Clear()
        {
            Values_.Clear();
            Ops_.Clear();
        }

        public bool Contains(T item)
        {
            return Values_.Contains(item);
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
                        Values_.Add(item);
                        break;
                    case OperationType.Remove:
                        Values_.Remove(item);
                        break;
                }
            }
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