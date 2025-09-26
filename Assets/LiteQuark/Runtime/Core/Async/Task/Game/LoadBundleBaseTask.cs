using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class LoadBundleBaseTask : BaseTask
    {
        protected readonly string BundleUri;
        private AssetBundle _bundle;
        private Action<AssetBundle> _callback;

        protected LoadBundleBaseTask(string bundleUri, Action<AssetBundle> callback)
            : base()
        {
            BundleUri = bundleUri;
            _bundle = null;
            _callback = callback;
        }
        
        public override void Dispose()
        {
            _callback = null;
        }

        protected void OnBundleLoaded(AssetBundle bundle)
        {
            _bundle = bundle;
            
            _callback?.Invoke(bundle);
            Complete(bundle);
        }

        public AssetBundle GetBundle()
        {
            return _bundle;
        }
    }
}