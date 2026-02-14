using System;
using System.Collections;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    [Obsolete]
    public class ListEx<T> : IEnumerable<T>
    {
        private bool _dirty;
        private int _inEach;

        private readonly List<T> _values;
        private readonly List<T> _addList;
        private readonly List<T> _removeList;

        public int Count => _addList.Count + _values.Count - _removeList.Count;

        public T this[int index] => _values[index];

        public ListEx()
        {
            _dirty = false;
            _inEach = 0;

            _values = new List<T>();
            _addList = new List<T>();
            _removeList = new List<T>();
        }

        public void Add(T item)
        {
            if (_removeList.Remove(item))
            {
                _dirty = true;
                return;
            }
            
            _addList.Add(item);
            _dirty = true;
        }

        public void Remove(T item)
        {
            if (_addList.Remove(item))
            {
                _dirty = true;
                return;
            }
            
            _removeList.Add(item);
            _dirty = true;
        }

        public void Clear()
        {
            _removeList.Clear();
            // AddList_.Clear();
            _values.Clear();
            _dirty = false;
        }

        public bool Contains(T item)
        {
            return _values.Contains(item) || _addList.Contains(item);
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

                foreach (var item in _addList)
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
                
                foreach (var item in _addList)
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
            if (!_dirty || _inEach > 0)
            {
                return;
            }
            
            foreach (var item in _removeList)
            {
                _values.Remove(item);
            }
            _removeList.Clear();
            
            foreach (var item in _addList)
            {
                _values.Add(item);
            }
            _addList.Clear();
            
            _dirty = false;
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