using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal sealed class LoadResourceTask<T> : BaseTask where T : UnityEngine.Object
    {
        public override string DebugName => $"LoadResource {_assetName}";
        
        private readonly string _assetName;
        private Action<T> _callback;
        private ResourceRequest _resourceRequest;
        private UnityEngine.Object _asset;

        public LoadResourceTask(string assetName, Action<T> callback)
            : base()
        {
            _assetName = assetName;
            _callback = callback;
            _resourceRequest = null;
            _asset = null;
        }

        public override void Dispose()
        {
            _callback = null;
            _resourceRequest = null;
            _asset = null;
        }
        
        protected override void OnTick(float deltaTime)
        {
            Progress = _resourceRequest?.progress ?? 0f;
        }
        
        protected override void OnExecute()
        {
            var path = PathUtils.GetFilePathWithoutExt(_assetName);
            _resourceRequest = Resources.LoadAsync<T>(path);
            _resourceRequest.completed += OnResourceRequestLoadCompleted;
        }

        private void OnResourceRequestLoadCompleted(AsyncOperation op)
        {
            op.completed -= OnResourceRequestLoadCompleted;
            
            if (IsDone)
            {
                return;
            }
            
            _asset = _resourceRequest.asset;
            Complete(_asset);
            LiteUtils.SafeInvoke(_callback, _asset as T);
        }
    }
}