using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal class AssetBundleLoader : ITick, IDisposeAsync
    {
        private readonly IBundleLocater _bundleLocater;
        private readonly VersionPackInfo _packInfo;
        private readonly int _concurrencyLimit;

        private readonly Dictionary<string, PendingTask> _taskMap;
        private readonly PriorityQueue<PendingTask> _pendingTaskQueue;
        private readonly List<RunningTask> _runningTasks;
        private readonly List<UniTask> _pendingCleanup = new();
        private bool _isDisposed;

        public AssetBundleLoader(IBundleLocater locater, VersionPackInfo packInfo, int concurrencyLimit)
        {
            _bundleLocater = locater ?? throw new ArgumentNullException(nameof(locater));
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

            foreach (var task in _runningTasks)
            {
                task.LoadTask.Cancel();
                RetainCleanup(task.LoadTask);
            }
            _runningTasks.Clear();
            try
            {
                await UniTaskUtils.WhenAllCompleteAsync(_pendingCleanup);
            }
            finally
            {
                _pendingCleanup.Clear();
            }
        }

        private void RetainCleanup(ILoadBundleTask task)
        {
            if (task is IResourceTaskCleanup resourceTask)
            {
                var cleanup = resourceTask.WaitForCleanupAsync();
                if (cleanup.Status != UniTaskStatus.Succeeded)
                {
                    // 归还 Loader 名额后，仍保留未结束或失败的原生收尾。
                    _pendingCleanup.Add(cleanup);
                }
            }
        }

        public void LoadBundle(BundleInfo bundleInfo, AssetLoadPriority priority, Action<AssetLoadResult<AssetBundle>> callback)
        {
            if (_isDisposed)
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<AssetBundle>.Failure(new AssetError(bundleInfo.BundlePath, "Bundle loader is disposed")));
                return;
            }

            var bundlePath = _packInfo.GetBundleFileLoadPath(bundleInfo);

            if (string.IsNullOrEmpty(bundlePath))
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<AssetBundle>.Failure(new AssetError(bundleInfo.BundlePath, "Bundle load path is missing")));
                return;
            }

            if (_taskMap.TryGetValue(bundlePath, out var task))
            {
                task.CallbackList.Add(callback);
                RaisePendingPriority(task, priority);
                return;
            }

            task = new PendingTask(bundlePath, bundleInfo.BundlePath, bundleInfo.Hash, priority, callback);
            _taskMap.Add(bundlePath, task);
            _pendingTaskQueue.Enqueue(task);
        }

        public void RaiseBundlePriority(BundleInfo bundleInfo, AssetLoadPriority priority)
        {
            if (_isDisposed)
            {
                return;
            }

            var bundlePath = _packInfo.GetBundleFileLoadPath(bundleInfo);

            if (string.IsNullOrEmpty(bundlePath))
            {
                return;
            }

            if (_taskMap.TryGetValue(bundlePath, out var task))
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
            _pendingCleanup.RemoveAll(static cleanup => cleanup.Status == UniTaskStatus.Succeeded);
            for (var i = _runningTasks.Count - 1; i >= 0 && !_isDisposed; i--)
            {
                var runningTask = _runningTasks[i];
                if (runningTask.LoadTask.IsDone)
                {
                    RetainCleanup(runningTask.LoadTask);
                    _runningTasks.RemoveAt(i);

                    // Cancel 不发送结果回调，退休时结束对应请求。
                    if (runningTask.LoadTask.State == TaskState.Cancelled)
                    {
                        CompletePendingTask(runningTask.Path, null, runningTask.LoadTask.Error);
                    }
                }
            }

            while (!_isDisposed && _runningTasks.Count < _concurrencyLimit && _pendingTaskQueue.Count > 0)
            {
                var info = _pendingTaskQueue.Dequeue();
                ILoadBundleTask task = null;

                try
                {
                    task = _bundleLocater.LoadBundle(info.Path, info.Hash,
                        bundle => CompletePendingTask(info.Path, bundle, task?.Error));
                }
                catch (Exception ex)
                {
                    LLog.Exception(ex, "Create bundle load task failed : {0}", info.Path);
                    CompletePendingTask(info.Path, null, ex);
                    break;
                }

                _runningTasks.Add(new RunningTask(info.Path, task));
            }
        }

        private void CompletePendingTask(string bundlePath, AssetBundle bundle, Exception error)
        {
            // Complete 会先恢复 Task 续体；若续体已关闭 Loader，结果不能再交付。
            if (_isDisposed)
            {
                if (bundle != null)
                {
                    bundle.Unload(true);
                }
                return;
            }

            if (!_taskMap.Remove(bundlePath, out var pendingTask))
            {
                return;
            }

            var result = bundle != null
                ? AssetLoadResult<AssetBundle>.Success(bundle)
                : AssetLoadResult<AssetBundle>.Failure(new AssetError(pendingTask.BundlePath,
                    error?.Message ?? "Bundle load returned no result", error));
            foreach (var callback in pendingTask.CallbackList)
            {
                LiteUtils.SafeInvoke(callback, result);
            }
        }

        private class PendingTask
        {
            public string Path { get; }
            public string BundlePath { get; }
            public string Hash { get; }
            public AssetLoadPriority Priority { get; set; }
            public List<Action<AssetLoadResult<AssetBundle>>> CallbackList { get; set; }

            public PendingTask(string path, string bundlePath, string hash, AssetLoadPriority priority, Action<AssetLoadResult<AssetBundle>> callback)
            {
                Path = path;
                BundlePath = bundlePath;
                Hash = hash;
                Priority = priority;
                CallbackList = new List<Action<AssetLoadResult<AssetBundle>>> { callback };
            }
        }

        private class RunningTask
        {
            public string Path { get; }
            public ILoadBundleTask LoadTask { get; }

            public RunningTask(string path, ILoadBundleTask loadTask)
            {
                Path = path;
                LoadTask = loadTask;
            }
        }
    }
}
