using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class LoadAssetBaseTask : BaseTask
    {
        protected readonly AssetBundle Bundle;
        protected readonly string Name;
        protected Action<UnityEngine.Object> Callback;
        
        protected LoadAssetBaseTask(AssetBundle bundle, string name, Action<UnityEngine.Object> callback)
        {
            Bundle = bundle;
            Name = name;
            Callback = callback;
        }
        
        public override void Dispose()
        {
            Callback = null;
        }
        
        public abstract UnityEngine.Object WaitCompleted();
    }
}