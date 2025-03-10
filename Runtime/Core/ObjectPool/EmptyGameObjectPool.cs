using UnityEngine;

namespace LiteQuark.Runtime
{
    public class EmptyGameObjectPool : BaseGameObjectPool
    {
        public override string Name => Key;
        
        public EmptyGameObjectPool()
        {
        }

        public override void Initialize(string key, params object[] args)
        {
            base.Initialize(key, args);
            
            OnLoadTemplate(new GameObject("PoolTemplate"));
        }
        
        public override void Dispose()
        {
            if (Template_ != null)
            {
                Object.DestroyImmediate(Template_);
                Template_ = null;
            }
            
            base.Dispose();
        }
    }
}