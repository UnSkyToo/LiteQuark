using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed class LoadLocalBundleTask : BaseTask, ILoadBundleTask, IResourceTaskCleanup
    {
        public override string DebugName => $"LoadBundle {PathUtils.GetFileName(_bundleUri)}";

        private readonly string _bundleUri;
        private AssetBundleCreateRequest _bundleRequest;
        private readonly UniTaskCompletionSource _cleanupCompletion = new();

        public UniTask WaitForCleanupAsync() => _cleanupCompletion.Task;

        public LoadLocalBundleTask(string bundleUri)
            : base()
        {
            _bundleUri = bundleUri;
        }

        protected override void OnTerminated()
        {
            if (_bundleRequest == null && State != TaskState.Succeeded)
            {
                _cleanupCompletion.TrySetResult();
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

            try
            {
                var bundle = _bundleRequest.assetBundle;
                if (State is TaskState.Failed or TaskState.Cancelled)
                {
                    if (bundle != null)
                    {
                        bundle.Unload(true);
                    }
                    return;
                }

                if (bundle == null)
                {
                    Fail(new InvalidOperationException($"Bundle load returned no bundle: {_bundleUri}"));
                    return;
                }

                var resource = new AssetBundleResource(bundle);
                if (!Complete(resource))
                {
                    resource.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (IsDone)
                {
                    _cleanupCompletion.TrySetException(ex);
                    LLog.Exception(ex, "Release late bundle failed : {0}", _bundleUri);
                }
                else
                {
                    Fail(ex);
                }
            }
            finally
            {
                _bundleRequest = null;
                _cleanupCompletion.TrySetResult();
            }
        }

        public IBundleResource GetBundle() => Result as IBundleResource;
    }
}
