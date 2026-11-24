using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal class AssetBundleLoader : ITick, IDisposeAsync
    {
        private readonly IBundleProvider _bundleProvider;
        private readonly VersionPackInfo _packInfo;
        private readonly int _concurrencyLimit;

        // 按逻辑 Bundle 定位调度项；共享请求由 Cache 合并，每项只有一个接收方。
        private readonly Dictionary<string, PendingTask> _taskMap;
        private readonly PriorityQueue<PendingTask> _pendingTaskQueue;
        private readonly List<RunningTask> _runningTasks;
        private bool _isDisposed;

        public AssetBundleLoader(IBundleProvider provider, VersionPackInfo packInfo, int concurrencyLimit)
        {
            _bundleProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            _packInfo = packInfo ?? throw new ArgumentNullException(nameof(packInfo));
            _concurrencyLimit = Mathf.Max(1, concurrencyLimit);

            _taskMap = new Dictionary<string, PendingTask>();
            _pendingTaskQueue = new PriorityQueue<PendingTask>(static (x, y) => ((int)x.Priority).CompareTo((int)y.Priority));
            _runningTasks = new List<RunningTask>();
        }

        public async UniTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            _taskMap.Clear();
            _pendingTaskQueue.Clear();

            var stops = new UniTask[_runningTasks.Count];
            for (var i = 0; i < stops.Length; i++)
            {
                stops[i] = StopAndReleaseAsync(_runningTasks[i].LoadTask);
            }
            _runningTasks.Clear();
            await UniTask.WhenAll(stops);
        }

        private static async UniTask StopAndReleaseAsync(ILoadBundleTask task)
        {
            if (task is LoadLocalBundleTask && task.State == TaskState.Running)
            {
                // 已提交的本地读取由 Unity 自行完成，等结果交还后再卸包。
                await task.Task;
            }
            else if (!task.IsDone)
            {
                task.Cancel();
            }
            task.GetBundle()?.Dispose();
        }

        public void LoadBundle(BundleInfo bundleInfo, AssetLoadPriority priority, Action<AssetLoadResult<IBundleResource>> callback)
        {
            if (_isDisposed)
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<IBundleResource>.Failure(new AssetError(bundleInfo.BundlePath, "Bundle loader is disposed")));
                return;
            }

            var bundlePath = _packInfo.GetBundleFileLoadPath(bundleInfo);

            if (string.IsNullOrEmpty(bundlePath))
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<IBundleResource>.Failure(new AssetError(bundleInfo.BundlePath, "Bundle load path is missing")));
                return;
            }

            var task = new PendingTask(bundlePath, bundleInfo.BundlePath, bundleInfo.Hash, priority, callback);
            _taskMap.Add(bundleInfo.BundlePath, task);
            _pendingTaskQueue.Enqueue(task);
        }

        public void RaiseBundlePriority(BundleInfo bundleInfo, AssetLoadPriority priority)
        {
            if (_isDisposed)
            {
                return;
            }

            if (_taskMap.TryGetValue(bundleInfo.BundlePath, out var task))
            {
                RaisePendingPriority(task, priority);
            }
        }

        private void RaisePendingPriority(PendingTask task, AssetLoadPriority priority)
        {
            if (priority <= task.Priority)
            {
                return;
            }

            if (!_pendingTaskQueue.Contains(task))
            {
                return;
            }

            task.Priority = priority;
            _pendingTaskQueue.UpdatePriority(task);
        }

        public void Tick(float deltaTime)
        {
            if (_isDisposed)
            {
                return;
            }
            for (var i = _runningTasks.Count - 1; i >= 0 && !_isDisposed; i--)
            {
                var running = _runningTasks[i];
                var task = running.LoadTask;
                if (!task.IsDone)
                {
                    continue;
                }

                // 交付前移除记录，回调可以在当前栈上关闭 Loader。
                _runningTasks.RemoveAt(i);
                CompletePendingTask(running.BundlePath, task.GetBundle(), task.Error);
            }

            while (!_isDisposed && _runningTasks.Count < _concurrencyLimit && _pendingTaskQueue.Count > 0)
            {
                var info = _pendingTaskQueue.Dequeue();
                ILoadBundleTask task;

                try
                {
                    task = _bundleProvider.LoadBundle(info.Path, info.Hash);
                }
                catch (Exception ex)
                {
                    LLog.Exception(ex, "Create bundle load task failed : {0}", info.Path);
                    CompletePendingTask(info.BundlePath, null, ex);
                    break;
                }

                _runningTasks.Add(new RunningTask(info.BundlePath, task));
            }
        }

        private void CompletePendingTask(string bundlePath, IBundleResource bundle, Exception error)
        {
            if (!_taskMap.Remove(bundlePath, out var pendingTask))
            {
                return;
            }

            var result = bundle != null
                ? AssetLoadResult<IBundleResource>.Success(bundle)
                : AssetLoadResult<IBundleResource>.Failure(new AssetError(pendingTask.BundlePath,
                    error?.Message ?? "Bundle load returned no result", error));
            LiteUtils.SafeInvoke(pendingTask.Callback, result);
        }

        private class PendingTask
        {
            public string Path { get; }
            public string BundlePath { get; }
            public string Hash { get; }
            public AssetLoadPriority Priority { get; set; }
            public Action<AssetLoadResult<IBundleResource>> Callback { get; }

            public PendingTask(string path, string bundlePath, string hash, AssetLoadPriority priority, Action<AssetLoadResult<IBundleResource>> callback)
            {
                Path = path;
                BundlePath = bundlePath;
                Hash = hash;
                Priority = priority;
                Callback = callback;
            }
        }

        private class RunningTask
        {
            public string BundlePath { get; }
            public ILoadBundleTask LoadTask { get; }

            public RunningTask(string bundlePath, ILoadBundleTask loadTask)
            {
                BundlePath = bundlePath;
                LoadTask = loadTask;
            }
        }
    }
}
