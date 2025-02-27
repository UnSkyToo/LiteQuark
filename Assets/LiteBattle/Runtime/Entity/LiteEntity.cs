using System;
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
        private string PrefabPath_;

        private readonly LiteContext Context_;
        private readonly Dictionary<string, LiteEntityModuleBase> Modules_;

        protected LiteEntity()
            : base()
        {
            Modules_ = new Dictionary<string, LiteEntityModuleBase>();
            Context_ = new LiteContext(LiteBattleEngine.Instance.GlobalContext);

            Camp = LiteEntityCamp.Light;
            
            SetTag(LiteTag.CanMove, true);
            SetTag(LiteTag.CanJump, true);
            SetTag(LiteTag.Hit, false);

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

        private string GetModuleKey<T>() where T : LiteEntityModuleBase
        {
            return typeof(T).Name;
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
                module = Activator.CreateInstance(typeof(T), this) as T;
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

        public GameObject GetGo()
        {
            return Go_;
        }

        public void LoadPrefab(string prefabPath)
        {
            LoadPrefab(prefabPath, Vector3.zero, Vector3.one, Quaternion.identity);
        }

        public void LoadPrefab(string prefabPath, Vector3 position, Vector3 scale, Quaternion rotation)
        {
            RecyclePrefab();
            PrefabPath_ = prefabPath;
            
            Go_ = LiteRuntime.ObjectPool.GetActiveGameObjectPool(PrefabPath_).Alloc();
            Go_.transform.localPosition = position;
            Go_.transform.localScale = scale;
            Go_.transform.localRotation = rotation;

            ColliderBinder = Go_.GetOrAddComponent<LiteColliderBinder>();
            if (ColliderBinder != null)
            {
                ColliderBinder.EntityUniqueID = UniqueID;
            }
        }

        private void RecyclePrefab()
        {
            if (Go_ != null)
            {
                LiteRuntime.ObjectPool.GetActiveGameObjectPool(PrefabPath_).Recycle(Go_);
                Go_ = null;
            }

            PrefabPath_ = string.Empty;
        }
    }
}