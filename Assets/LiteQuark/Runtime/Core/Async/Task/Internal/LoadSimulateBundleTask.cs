#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using LiteQuark.Editor;

namespace LiteQuark.Runtime
{
    internal sealed class LoadSimulateBundleTask : BaseTask, IResourceTaskCleanup
    {
        public override string DebugName => $"SimulateBundle {_bundleInfo.BundlePath}";

        private readonly BundleInfo _bundleInfo;
        private readonly ResCollector _collector;
        private Action<IBundleResource> _callback;
        private IBundleResource _resource;
        private readonly UniTaskCompletionSource _cleanupCompletion = new();

        public LoadSimulateBundleTask(BundleInfo bundleInfo, ResCollector collector, Action<IBundleResource> callback)
        {
            _bundleInfo = bundleInfo;
            _collector = collector;
            _callback = callback;
        }

        public UniTask WaitForCleanupAsync() => _cleanupCompletion.Task;

        protected override void OnExecute()
        {
            _resource = new SimulateBundleResource(_bundleInfo, _collector);
        }

        protected override void OnTick(float deltaTime)
        {
            var resource = _resource;
            _resource = null;
            var callback = _callback;
            _callback = null;
            try
            {
                if (Complete(resource))
                {
                    LiteUtils.SafeInvoke(callback, resource);
                }
            }
            finally
            {
                _cleanupCompletion.TrySetResult();
            }
        }

        protected override void OnTerminated()
        {
            if (State == TaskState.Succeeded)
            {
                return;
            }
            _resource?.Dispose();
            _resource = null;
            var callback = _callback;
            _callback = null;
            if (State == TaskState.Failed)
            {
                LiteUtils.SafeInvoke(callback, null);
            }
            _cleanupCompletion.TrySetResult();
        }

        protected override void OnDispose()
        {
            _callback = null;
        }
    }
}
#endif
