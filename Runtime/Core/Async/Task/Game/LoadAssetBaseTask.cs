using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class LoadAssetBaseTask : BaseTask
    {
        protected readonly AssetBundle Bundle_;
        protected readonly string Name_;
        protected Action<UnityEngine.Object> Callback_;
        
        protected LoadAssetBaseTask(AssetBundle bundle, string name, Action<UnityEngine.Object> callback)
        {
            Bundle_ = bundle;
            Name_ = name;
            Callback_ = callback;
        }
        
        public override void Dispose()
        {
            Callback_ = null;
        }
        
        public abstract UnityEngine.Object WaitCompleted();
    }
}