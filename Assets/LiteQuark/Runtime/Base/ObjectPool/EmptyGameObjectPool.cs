using UnityEngine;
using UnityEngine.Pool;

namespace LiteQuark.Runtime
{
    public class EmptyGameObjectPool : IGameObjectPool
    {
        public string Key => Key_;
        public string Name => Key_;
        public int CountAll => Pool_?.CountAll ?? 0;
        public int CountActive => Pool_?.CountActive ?? 0;
        public int CountInactive => Pool_?.CountInactive ?? 0;

        protected string Key_;
        protected Transform Parent_;
        protected ObjectPool<GameObject> Pool_;
        
        public EmptyGameObjectPool()
        {
        }

        public void Initialize(string key, params object[] args)
        {
            Key_ = key;
            Parent_ = new GameObject(key).transform;
            Parent_.hideFlags = HideFlags.NotEditable;
            var root = args.Length > 0 && args[0] is Transform ? (Transform)args[0] : null;
            if (root != null)
            {
                Parent_.SetParent(root, false);
            }
            Parent_.SetParent(root, false);
            Parent_.localPosition = Vector3.zero;
            Pool_ = new ObjectPool<GameObject>(OnCreate, OnGet, OnRelease, OnDestroy);
        }
        
        public virtual void Dispose()
        {
            Pool_.Dispose();

            if (Parent_ != null)
            {
                Object.DestroyImmediate(Parent_.gameObject);
                Parent_ = null;
            }
        }

        protected virtual GameObject OnCreate()
        {
            var go = new GameObject("Empty");
            return go;
        }

        protected virtual void OnGet(GameObject go)
        {
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        protected virtual void OnRelease(GameObject go)
        {
            go.transform.SetParent(Parent_, false);
        }

        protected virtual void OnDestroy(GameObject go)
        {
            Object.DestroyImmediate(go);
        }

        public virtual void Generate(int count)
        {
            var go = new GameObject[count];
            
            for (var i = 0; i < count; ++i)
            {
                go[i] = Alloc(Parent_);
            }

            for (var i = 0; i < count; ++i)
            {
                Recycle(go[i]);
            }
        }

        public virtual GameObject Alloc()
        {
            return Alloc(null);
        }

        public virtual GameObject Alloc(Transform parent)
        {
            var go = Pool_.Get();
            go.transform.SetParent(parent, false);
            return go;
        }

        public virtual void Recycle(GameObject value)
        {
            Pool_.Release(value);
        }
    }
}