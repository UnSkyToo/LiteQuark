using UnityEngine;
using UnityEngine.Pool;

namespace LiteQuark.Runtime
{
    public class GameObjectPool : IGameObjectPool
    {
        public string Key => Path_;
        public string Name => PathUtils.GetFileName(Path_);
        public int CountAll => Pool_?.CountAll ?? 0;
        public int CountActive => Pool_?.CountActive ?? 0;
        public int CountInactive => Pool_?.CountInactive ?? 0;
        
        private static readonly Vector3 InvalidPosition = new Vector3(-9999, -9999, -9999);

        protected string Path_;
        protected Transform Parent_;
        protected GameObject Template_;
        protected ObjectPool<GameObject> Pool_;
        
        public GameObjectPool()
        {
        }

        public void Initialize(string key, params object[] args)
        {
            Path_ = key;
            Parent_ = new GameObject(Path_).transform;
            Parent_.hideFlags = HideFlags.NotEditable;
            var root = args.Length > 0 && args[0] is Transform ? (Transform)args[0] : null;
            if (root != null)
            {
                Parent_.SetParent(root, false);
            }
            Parent_.localPosition = Vector3.zero;
            Template_ = LiteRuntime.Asset.LoadAssetSync<GameObject>(Path_);
            Pool_ = new ObjectPool<GameObject>(OnCreate, OnGet, OnRelease, OnDestroy);
        }
        
        public virtual void Dispose()
        {
            Pool_.Dispose();

            if (Template_ != null)
            {
                LiteRuntime.Asset.UnloadAsset(Template_);
                Template_ = null;
            }

            if (Parent_ != null)
            {
                Object.DestroyImmediate(Parent_.gameObject);
                Parent_ = null;
            }
        }

        protected virtual GameObject OnCreate()
        {
            var go = Object.Instantiate(Template_, Parent_);
            return go;
        }

        protected virtual void OnGet(GameObject go)
        {
        }

        protected virtual void OnRelease(GameObject go)
        {
            go.transform.SetParent(Parent_, false);
            go.transform.localPosition = InvalidPosition;
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