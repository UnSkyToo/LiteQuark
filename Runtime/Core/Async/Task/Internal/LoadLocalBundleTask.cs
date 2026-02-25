using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal sealed class LoadLocalBundleTask : BaseTask, ILoadBundleTask
    {
        public override string DebugName => $"LoadBundle {PathUtils.GetFileName(_bundleUri)}";
        
        private readonly string _bundleUri;
        private AssetBundle _bundle;
        private Action<AssetBundle> _callback;
        private AssetBundleCreateRequest _bundleRequest;
        
        public LoadLocalBundleTask(string bundleUri, Action<AssetBundle> callback)
            : base()
        {
            _bundleUri = bundleUri;
            _bundle = null;
            _callback = callback;
        }
        
        public override void Dispose()
        {
            _callback = null;
        }
        
        protected override void OnTick(float deltaTime)
        {
            Progress = _bundleRequest?.progress ?? 0f;
        }

        protected override void OnExecute()
        {
            _bundleRequest = AssetBundle.LoadFromFileAsync(_bundleUri);
            _bundleRequest.completed += OnBundleRequestCompleted;
        }
        
        private void OnBundleRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;
            
            if (IsDone)
            {
                return;
            }
            
            _bundle = _bundleRequest.assetBundle;
            Complete(_bundle);
            LiteUtils.SafeInvoke(_callback, _bundle);
        }

        public AssetBundle GetBundle() => _bundle;
    }
}