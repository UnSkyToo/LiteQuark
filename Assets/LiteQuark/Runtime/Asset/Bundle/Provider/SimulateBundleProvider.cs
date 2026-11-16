#if UNITY_EDITOR

namespace LiteQuark.Runtime
{
    internal sealed class SimulateBundleProvider : IBundleProvider
    {
        private LoadSimulateVersionPackTask _versionTask;

        public BaseTask LoadVersionPack(string versionFileName)
        {
            _versionTask = new LoadSimulateVersionPackTask();
            LiteRuntime.Task.AddTask(_versionTask, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
            return _versionTask;
        }

        public ILoadBundleTask LoadBundle(string bundlePath, string hash)
        {
            // 清单和源路径快照均在恢复初始化等待者之前发布。
            var pack = (VersionPackInfo)_versionTask.Result;
            var task = new LoadSimulateBundleTask(pack.GetBundleInfoFromBundlePath(bundlePath), _versionTask.AssetSourcePaths);
            LiteRuntime.Task.AddTask(task, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
            return task;
        }
    }
}
#endif
