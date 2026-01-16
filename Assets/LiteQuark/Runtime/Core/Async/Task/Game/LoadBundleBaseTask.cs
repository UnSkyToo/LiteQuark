using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class LoadBundleBaseTask : BaseTask
    {
        public override string DebugName => $"LoadBundle {PathUtils.GetFileName(BundleUri)}";

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
            
            LiteUtils.SafeInvoke(_callback, bundle);
            Complete(bundle);
        }

        public AssetBundle GetBundle()
        {
            return _bundle;
        }
    }
}