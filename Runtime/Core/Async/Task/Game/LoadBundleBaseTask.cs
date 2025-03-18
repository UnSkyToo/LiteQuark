using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class LoadBundleBaseTask : BaseTask
    {
        protected Action<AssetBundle> Callback_;

        protected LoadBundleBaseTask(Action<AssetBundle> callback)
            : base()
        {
            Callback_ = callback;
        }
        
        public override void Dispose()
        {
            Callback_ = null;
        }

        public abstract AssetBundle WaitCompleted();
    }
}