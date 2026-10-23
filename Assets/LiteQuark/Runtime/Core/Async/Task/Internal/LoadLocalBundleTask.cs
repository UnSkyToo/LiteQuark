using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal sealed class LoadLocalBundleTask : BaseTask, ILoadBundleTask
    {
        public override string DebugName => $"LoadBundle {PathUtils.GetFileName(_bundleUri)}";

        private readonly string _bundleUri;
        private Action<AssetBundle> _callback;
        private AssetBundleCreateRequest _bundleRequest;

        public LoadLocalBundleTask(string bundleUri, Action<AssetBundle> callback)
            : base()
        {
            _bundleUri = bundleUri;
            _callback = callback;
        }

        protected override void OnDispose()
        {
            _callback = null;
        }

        protected override void OnTerminated()
        {
            if (State == TaskState.Failed)
            {
                var callback = _callback;
                _callback = null;
                LiteUtils.SafeInvoke(callback, null);
            }
        }

        protected override void OnTick(float deltaTime)
        {
            Progress = _bundleRequest?.progress ?? 0f;
        }

        protected override void OnExecute()
        {
            _bundleRequest = AssetBundle.LoadFromFileAsync(_bundleUri);
            if (_bundleRequest == null)
            {
                Fail(new InvalidOperationException($"Could not start bundle load: {_bundleUri}"));
                return;
            }
            _bundleRequest.completed += OnBundleRequestCompleted;
        }

        private void OnBundleRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;

            if (IsDone)
            {
                return;
            }

            AssetBundle bundle;
            try
            {
                bundle = _bundleRequest.assetBundle;
                if (bundle == null)
                {
                    Fail(new InvalidOperationException($"Bundle load returned no bundle: {_bundleUri}"));
                    return;
                }
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }

            if (Complete(bundle))
            {
                var callback = _callback;
                _callback = null;
                LiteUtils.SafeInvoke(callback, bundle);
            }
        }

        public AssetBundle GetBundle() => Result as AssetBundle;
    }
}
