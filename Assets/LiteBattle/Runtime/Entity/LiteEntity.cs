using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public abstract class LiteEntity : BaseObject, ISubstance
    {
        public string Tag { get; set; }
        
        public bool IsAlive { get; protected set; }
        public LiteEntityCamp Camp { get; set; }
        public LiteColliderBinder ColliderBinder { get; protected set; }
        
        public Vector3 Position
        {
            get => Go_.transform.localPosition;
            set => Go_.transform.localPosition = value;
        }

        public Vector3 Scale
        {
            get => Go_.transform.localScale;
            set => Go_.transform.localScale = value;
        }

        public Quaternion Rotation
        {
            get => Go_.transform.localRotation;
            set => Go_.transform.localRotation = value;
        }
        
        private GameObject Go_;
        private GameObject InternalGo_;
        private string PrefabPath_;

        private readonly LiteContext Context_;
        private readonly Dictionary<System.Type, LiteEntityModuleBase> Modules_;

        protected LiteEntity()
            : base()
        {
            Modules_ = new Dictionary<System.Type, LiteEntityModuleBase>();
            Context_ = new LiteContext(LiteNexusEngine.Instance.GlobalContext);

            Camp = LiteEntityCamp.Light;
            IsAlive = true;
        }

        public virtual void Dispose()
        {
            foreach (var module in Modules_)
            {
                module.Value.Dispose();
            }
            Modules_.Clear();
            
            RecyclePrefab();

            if (Go_ != null)
            {
                Object.DestroyImmediate(Go_);
                Go_ = null;
            }
        }

        public virtual void Tick(float deltaTime)
        {
            foreach (var module in Modules_)
            {
                module.Value.Tick(deltaTime);
            }
        }

        public virtual void PostTick(float deltaTime)
        {
            Context_.Tick();
        }

        private System.Type GetModuleKey<T>() where T : LiteEntityModuleBase
        {
            return typeof(T);
        }

        public T GetModule<T>() where T : LiteEntityModuleBase
        {
            var key = GetModuleKey<T>();
            if (Modules_.TryGetValue(key, out var value))
            {
                return value as T;
            }

            return default;
        }

        public T AttachModule<T>() where T : LiteEntityModuleBase
        {
            var module = GetModule<T>();
            if (module != null)
            {
                return module;
            }

            try
            {
                var key = GetModuleKey<T>();
                module = System.Activator.CreateInstance(typeof(T), this) as T;
                Modules_.Add(key, module);
            }
            catch
            {
                LLog.Error($"can create instance : {typeof(T)}, please check constructor param");
            }

            return module;
        }

        public void DetachModule<T>() where T : LiteEntityModuleBase
        {
            var key = GetModuleKey<T>();
            Modules_.Remove(key);
        }
        
        public T GetContext<T>(string key, T defaultValue)
        {
            return Context_.GetData(key, defaultValue);
        }

        public void SetContext<T>(string key, T value, int lifeCycle = int.MaxValue)
        {
            Context_.SetData(key, value, lifeCycle);
        }

        public void SetTag(LiteTag tag, bool value, int lifeCycle = int.MaxValue)
        {
            Context_.SetTag(tag, value, lifeCycle);
        }

        public bool GetTag(LiteTag tag)
        {
            return Context_.GetTag(tag);
        }

        public GameObject GetInternalGo()
        {
            return InternalGo_;
        }

        public T GetComponent<T>()
        {
            return InternalGo_.GetComponent<T>();
        }

        public int GetInstanceID()
        {
            return InternalGo_.GetInstanceID();
        }

        protected void LoadPrefab(string prefabPath, System.Action callback)
        {
            if (Go_ == null)
            {
                Go_ = new GameObject($"Entity{UniqueID}");
            }
            
            RecyclePrefab();
            PrefabPath_ = prefabPath;
            
            LiteRuntime.ObjectPool.GetActiveGameObjectPool(PrefabPath_).Alloc(Go_.transform, (go) =>
            {
                InternalGo_ = go;
                InternalGo_.transform.localPosition = Vector3.zero;
                InternalGo_.transform.localScale = Vector3.one;
                InternalGo_.transform.localRotation = Quaternion.identity;

                ColliderBinder = InternalGo_.GetOrAddComponent<LiteColliderBinder>();
                if (ColliderBinder != null)
                {
                    ColliderBinder.EntityUniqueID = UniqueID;
                }
                
                callback?.Invoke();
            });
        }

        private void RecyclePrefab()
        {
            if (InternalGo_ != null)
            {
                LiteRuntime.ObjectPool.GetActiveGameObjectPool(PrefabPath_).Recycle(InternalGo_);
                InternalGo_ = null;
            }

            PrefabPath_ = string.Empty;
        }
    }
}