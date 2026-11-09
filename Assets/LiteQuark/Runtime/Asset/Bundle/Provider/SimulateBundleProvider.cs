#if UNITY_EDITOR
using System;
using LiteQuark.Editor;

namespace LiteQuark.Runtime
{
    internal sealed class SimulateBundleProvider : IBundleProvider
    {
        private readonly ResCollector _collector = new();
        private BaseTask _versionTask;

        public BaseTask LoadVersionPack(string versionFileName, Action<VersionPackInfo> callback)
        {
            _versionTask = new LoadSimulateVersionPackTask(_collector, callback);
            LiteRuntime.Task.AddTask(_versionTask, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
            return _versionTask;
        }

        public BaseTask LoadBundle(string bundlePath, string hash, Action<IBundleResource> callback)
        {
            // Result 在恢复初始化等待者之前已发布，不依赖随后执行的版本任务回调。
            var pack = (VersionPackInfo)_versionTask.Result;
            var task = new LoadSimulateBundleTask(pack.GetBundleInfoFromBundlePath(bundlePath), _collector, callback);
            LiteRuntime.Task.AddTask(task, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
            return task;
        }
    }
}
#endif
