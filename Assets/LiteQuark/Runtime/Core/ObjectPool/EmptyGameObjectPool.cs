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
            Template.transform.SetParent(Parent, false);
        }
        
        public override void Dispose()
        {
            if (Template != null)
            {
                Object.Destroy(Template);
                Template = null;
            }
            
            base.Dispose();
        }
    }
}