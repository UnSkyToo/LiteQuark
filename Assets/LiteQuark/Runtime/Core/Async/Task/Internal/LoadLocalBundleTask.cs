using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal sealed class LoadLocalBundleTask : BaseTask, ILoadBundleTask
    {
        public override string DebugName => $"LoadBundle {PathUtils.GetFileName(_bundleUri)}";

        private readonly string _bundleUri;
        private AssetBundleCreateRequest _bundleRequest;

        public LoadLocalBundleTask(string bundleUri)
            : base()
        {
            _bundleUri = bundleUri;
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

            AssetBundle bundle = null;
            Exception error = null;
            try
            {
                bundle = ((AssetBundleCreateRequest)op).assetBundle;
                if (IsDone && bundle != null)
                {
                    bundle.Unload(true);
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }

            _bundleRequest = null;
            if (IsDone)
            {
                if (error != null) LLog.Exception(error, "Release late bundle failed : {0}", _bundleUri);
                return;
            }

            if (error != null || bundle == null)
            {
                Fail(error ?? new InvalidOperationException($"Bundle load returned no bundle: {_bundleUri}"));
                return;
            }
            Complete(new AssetBundleResource(bundle));
        }

        public IBundleResource GetBundle() => Result as IBundleResource;
    }
}
