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

        public Vector3 Position { get; set; } = Vector3.zero;
        public Vector3 Scale { get; set; } = Vector3.one;
        public Quaternion Rotation { get; set; } = Quaternion.identity;
        public int AnimationNameHash { get; private set; } = 0;
        
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

        public virtual void Initialize()
        {
        }

        public virtual void Dispose()
        {
            foreach (var module in Modules_)
            {
                module.Value.Dispose();
            }
            Modules_.Clear();
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
        
        public void PlayAnimation(string animationName)
        {
            AnimationNameHash = Animator.StringToHash(animationName);
        }
    }
}