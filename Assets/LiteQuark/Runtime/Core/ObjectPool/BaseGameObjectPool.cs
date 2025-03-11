using UnityEngine;
using UnityEngine.Pool;

namespace LiteQuark.Runtime
{
    public abstract class BaseGameObjectPool : IGameObjectPool
    {
        public string Key { get; private set; }
        public virtual string Name => Key;
        public int CountAll => Pool_?.CountAll ?? 0;
        public int CountActive => Pool_?.CountActive ?? 0;
        public int CountInactive => Pool_?.CountInactive ?? 0;

        protected Transform Parent_;
        protected GameObject Template_;
        protected ObjectPool<GameObject> Pool_;
        protected event System.Action LoadTemplateCallback_;
        
        protected BaseGameObjectPool()
        {
        }

        public virtual void Initialize(string key, params object[] args)
        {
            Key = key;
            Parent_ = new GameObject(Name).transform;
            Parent_.hideFlags = HideFlags.NotEditable;
            var root = args.Length > 0 && args[0] is Transform ? (Transform)args[0] : null;
            if (root != null)
            {
                Parent_.SetParent(root, false);
            }
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
            if (Template_ == null)
            {
                return null;
            }
            
            var go = Object.Instantiate(Template_, Parent_);
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

        public void Generate(int count, System.Action<IBasePool> callback)
        {
            RunWhenLoadTemplated(() =>
            {
                if (Template_ == null)
                {
                    callback?.Invoke(this);
                    return;
                }

                LiteRuntime.Task.InstantiateGoTask(Template_, Parent_, count, (list) =>
                {
                    foreach (var go in list)
                    {
                        if (go != null)
                        {
                            Recycle(go);
                        }
                    }

                    callback?.Invoke(this);
                });
            });
        }

        public virtual void Alloc(System.Action<GameObject> calllback)
        {
            Alloc(null, calllback);
        }

        public virtual void Alloc(Transform parent, System.Action<GameObject> callback)
        {
            RunWhenLoadTemplated(() =>
            {
                var go = Pool_.Get();
                if (go != null && parent != null)
                {
                    go.transform.SetParent(parent, false);
                }

                callback?.Invoke(go);
            });
        }

        public virtual void Recycle(GameObject value)
        {
            if (value == null)
            {
                return;
            }
            
            Pool_.Release(value);
        }

        protected void RunWhenLoadTemplated(System.Action callback)
        {
            if (Template_ != null)
            {
                callback?.Invoke();
            }
            else
            {
                LoadTemplateCallback_ += callback;
            }
        }

        protected void OnLoadTemplate(GameObject template)
        {
            Template_ = template;
            LoadTemplateCallback_?.Invoke();
        }
    }
}