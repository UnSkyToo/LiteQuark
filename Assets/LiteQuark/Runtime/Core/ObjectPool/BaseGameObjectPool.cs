using UnityEngine;
using UnityEngine.Pool;

namespace LiteQuark.Runtime
{
    public abstract class BaseGameObjectPool : IGameObjectPool
    {
        public string Key { get; private set; }
        public virtual string Name => Key;
        public int CountAll => Pool?.CountAll ?? 0;
        public int CountActive => Pool?.CountActive ?? 0;
        public int CountInactive => Pool?.CountInactive ?? 0;

        protected Transform Parent;
        protected GameObject Template;
        protected ObjectPool<GameObject> Pool;
        protected event System.Action LoadTemplateCallback;
        
        protected BaseGameObjectPool()
        {
        }

        public virtual void Initialize(string key, params object[] args)
        {
            Key = key;
            Parent = new GameObject(Name).transform;
            Parent.hideFlags = HideFlags.NotEditable;
            var root = args.Length > 0 && args[0] is Transform ? (Transform)args[0] : null;
            if (root != null)
            {
                Parent.SetParent(root, false);
            }
            Parent.localPosition = Vector3.zero;
            
            Pool = new ObjectPool<GameObject>(OnCreate, OnGet, OnRelease, OnDestroy);
        }
        
        public virtual void Dispose()
        {
            Pool.Dispose();

            if (Parent != null)
            {
                Object.DestroyImmediate(Parent.gameObject);
                Parent = null;
            }
        }

        protected virtual GameObject OnCreate()
        {
            if (Template == null)
            {
                return null;
            }
            
            var go = Object.Instantiate(Template, Parent);
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
            go.transform.SetParent(Parent, false);
        }

        protected virtual void OnDestroy(GameObject go)
        {
            Object.DestroyImmediate(go);
        }

        public void Generate(int count, System.Action<IBasePool> callback)
        {
            RunWhenLoadTemplated(() =>
            {
                if (Template == null)
                {
                    callback?.Invoke(this);
                    return;
                }

                LiteRuntime.Task.InstantiateGoTask(Template, Parent, count, (list) =>
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
                var go = Pool.Get();
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
            
            Pool.Release(value);
        }

        protected void RunWhenLoadTemplated(System.Action callback)
        {
            if (Template != null)
            {
                callback?.Invoke();
            }
            else
            {
                LoadTemplateCallback += callback;
            }
        }

        protected void OnLoadTemplate(GameObject template)
        {
            Template = template;
            LoadTemplateCallback?.Invoke();
        }
    }
}