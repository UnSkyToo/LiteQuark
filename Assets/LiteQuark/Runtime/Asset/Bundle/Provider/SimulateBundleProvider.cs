#if UNITY_EDITOR
using LiteQuark.Editor;

namespace LiteQuark.Runtime
{
    internal sealed class SimulateBundleProvider : IBundleProvider
    {
        private readonly ResCollector _collector = new();
        private BaseTask _versionTask;

        public BaseTask LoadVersionPack(string versionFileName)
        {
            _versionTask = new LoadSimulateVersionPackTask(_collector, null);
            LiteRuntime.Task.AddTask(_versionTask, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
            return _versionTask;
        }

        public ILoadBundleTask LoadBundle(string bundlePath, string hash)
        {
            // Result 在恢复初始化等待者之前已发布，不依赖随后执行的版本任务回调。
            var pack = (VersionPackInfo)_versionTask.Result;
            var task = new LoadSimulateBundleTask(pack.GetBundleInfoFromBundlePath(bundlePath), _collector);
            LiteRuntime.Task.AddTask(task, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
            return task;
        }
    }
}
#endif
