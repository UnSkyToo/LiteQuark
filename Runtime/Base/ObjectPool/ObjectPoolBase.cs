using System.Collections.Generic;
using System.Data;

namespace LiteQuark.Runtime
{
    public abstract class ObjectPoolBase<T> : IObjectPool
    {
        private readonly Stack<T> ObjectStack_;

        protected ObjectPoolBase()
        {
            ObjectStack_ = new Stack<T>();
        }

        public abstract void Initialize(object param);
        
        public T Alloc()
        {
            var obj = ObjectStack_.Count > 0 ? ObjectStack_.Pop() : OnCreate();
            if (obj == null)
            {
                throw new NoNullAllowedException("object pool create null obj");
            }
            OnAlloc(obj);
            return obj;
        }

        public void Recycle(T obj)
        {
            OnRecycle(obj);
            ObjectStack_.Push(obj);
        }
        
        public virtual void Clean()
        {
            foreach (var obj in ObjectStack_)
            {
                OnDelete(obj);
            }
            
            ObjectStack_.Clear();
        }

        protected abstract T OnCreate();
        protected abstract void OnAlloc(T obj);
        protected abstract void OnRecycle(T obj);
        protected abstract void OnDelete(T obj);
    }
}