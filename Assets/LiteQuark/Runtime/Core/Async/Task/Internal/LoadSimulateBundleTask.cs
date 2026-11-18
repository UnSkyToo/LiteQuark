#if UNITY_EDITOR
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed class LoadSimulateBundleTask : BaseTask, ILoadBundleTask
    {
        public override string DebugName => $"SimulateBundle {_bundleInfo.BundlePath}";

        private readonly BundleInfo _bundleInfo;
        private readonly IReadOnlyDictionary<string, string> _sourcePaths;
        private IBundleResource _resource;

        public LoadSimulateBundleTask(BundleInfo bundleInfo, IReadOnlyDictionary<string, string> sourcePaths)
        {
            _bundleInfo = bundleInfo;
            _sourcePaths = sourcePaths;
        }

        public IBundleResource GetBundle() => Result as IBundleResource;

        protected override void OnExecute()
        {
            _resource = new SimulateBundleResource(_bundleInfo, _sourcePaths);
        }

        protected override void OnTick(float deltaTime)
        {
            var resource = _resource;
            _resource = null;
            if (!Complete(resource))
            {
                resource.Dispose();
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
        }
    }
}
#endif
