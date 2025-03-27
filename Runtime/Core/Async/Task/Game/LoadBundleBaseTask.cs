using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class LoadBundleBaseTask : BaseTask
    {
        protected readonly string BundleUri_;
        protected Action<AssetBundle> Callback_;

        protected LoadBundleBaseTask(string bundleUri, Action<AssetBundle> callback)
            : base()
        {
            BundleUri_ = bundleUri;
            Callback_ = callback;
        }
        
        public override void Dispose()
        {
            Callback_ = null;
        }

        public abstract AssetBundle WaitCompleted();
    }
}