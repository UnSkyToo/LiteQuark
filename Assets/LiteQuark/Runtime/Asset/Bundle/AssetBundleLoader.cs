using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal class AssetBundleLoader : ITick, IDispose
    {
        private readonly IBundleLocater _bundleLocater;
        private readonly VersionPackInfo _packInfo;
        private readonly int _concurrencyLimit;

        private readonly Dictionary<string, PendingTask> _taskMap;
        private readonly PriorityQueue<PendingTask> _pendingTaskQueue;
        private readonly List<RunningTask> _runningTasks;

        public AssetBundleLoader(IBundleLocater locater, VersionPackInfo packInfo, int concurrencyLimit)
        {
            _bundleLocater = locater ?? throw new ArgumentNullException(nameof(locater));
            _packInfo = packInfo ?? throw new ArgumentNullException(nameof(packInfo));
            _concurrencyLimit = Mathf.Max(1, concurrencyLimit);

            _taskMap = new Dictionary<string, PendingTask>();
            _pendingTaskQueue = new PriorityQueue<PendingTask>(static (x, y) => ((int)x.Priority).CompareTo((int)y.Priority));
            _runningTasks = new List<RunningTask>();
        }

        public void Dispose()
        {
            _taskMap.Clear();
            _pendingTaskQueue.Clear();

            foreach (var task in _runningTasks)
            {
                task.LoadTask.Cancel();
            }
            _runningTasks.Clear();
        }

        public void LoadBundle(BundleInfo bundleInfo, AssetLoadPriority priority, Action<AssetBundle> callback)
        {
            var bundlePath = _packInfo.GetBundleFileLoadPath(bundleInfo);

            if (string.IsNullOrEmpty(bundlePath))
            {
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            if (_taskMap.TryGetValue(bundlePath, out var task))
            {
                task.CallbackList.Add(callback);
                RaisePendingPriority(task, priority);
                return;
            }

            task = new PendingTask(bundlePath, bundleInfo.Hash, priority, callback);
            _taskMap.Add(bundlePath, task);
            _pendingTaskQueue.Enqueue(task);
        }

        public void RaiseBundlePriority(BundleInfo bundleInfo, AssetLoadPriority priority)
        {
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
            for (var i = _runningTasks.Count - 1; i >= 0; i--)
            {
                var runningTask = _runningTasks[i];
                if (runningTask.LoadTask.IsDone)
                {
                    _runningTasks.RemoveAt(i);

                    var bundle = runningTask.LoadTask.GetBundle();
                    if (_taskMap.Remove(runningTask.Path, out var pendingTask))
                    {
                        foreach (var callback in pendingTask.CallbackList)
                        {
                            LiteUtils.SafeInvoke(callback, bundle);
                        }
                    }
                }
            }

            while (_runningTasks.Count < _concurrencyLimit && _pendingTaskQueue.Count > 0)
            {
                var info = _pendingTaskQueue.Dequeue();
                ILoadBundleTask task;

                try
                {
                    task = _bundleLocater.LoadBundle(info.Path, info.Hash, null);
                }
                catch (Exception ex)
                {
                    LLog.Exception(ex, "Create bundle load task failed : {0}", info.Path);
                    FailPendingTask(info.Path);
                    break;
                }

                _runningTasks.Add(new RunningTask(info.Path, task));
            }
        }

        private void FailPendingTask(string bundlePath)
        {
            if (!_taskMap.Remove(bundlePath, out var pendingTask))
            {
                return;
            }

            foreach (var callback in pendingTask.CallbackList)
            {
                LiteUtils.SafeInvoke(callback, null);
            }
        }

        private class PendingTask
        {
            public string Path { get; }
            public string Hash { get; }
            public AssetLoadPriority Priority { get; set; }
            public List<Action<AssetBundle>> CallbackList { get; set; }

            public PendingTask(string path, string hash, AssetLoadPriority priority, Action<AssetBundle> callback)
            {
                Path = path;
                Hash = hash;
                Priority = priority;
                CallbackList = new List<Action<AssetBundle>> { callback };
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
