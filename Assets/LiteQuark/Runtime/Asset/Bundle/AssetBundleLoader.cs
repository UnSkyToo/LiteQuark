using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal class AssetBundleLoader : ITick, IDispose
    {
        private readonly IBundleLocater _bundleLocater;
        private readonly int _concurrencyLimit;
        
        private readonly Dictionary<string, PendingTask> _pendingTaskMap;
        private readonly PriorityQueue<PendingTask> _pendingTaskQueue;
        private readonly List<RunningTask> _runningTasks;
        
        public AssetBundleLoader(IBundleLocater locater, int concurrencyLimit)
        {
            _bundleLocater = locater ?? throw new ArgumentNullException(nameof(locater));
            _concurrencyLimit = Mathf.Max(1, concurrencyLimit);
            
            _pendingTaskMap = new Dictionary<string, PendingTask>();
            _pendingTaskQueue = new PriorityQueue<PendingTask>((x, y) => x.Priority.CompareTo(y.Priority));
            _runningTasks = new List<RunningTask>();
        }

        public void Dispose()
        {
            _pendingTaskMap.Clear();
            _pendingTaskQueue.Clear();
            _runningTasks.Clear();
        }

        public void LoadBundle(string bundlePath, int priority, Action<AssetBundle> callback)
        {
            if (string.IsNullOrEmpty(bundlePath))
            {
                LiteUtils.SafeInvoke(callback, null);
                return;
            }
            
            if (_pendingTaskMap.TryGetValue(bundlePath, out var waitTask))
            {
                waitTask.Priority = Mathf.Max(waitTask.Priority, priority);
                waitTask.Callback += callback;
                _pendingTaskQueue.UpdatePriority(waitTask);
            }
            else
            {
                waitTask = new PendingTask(bundlePath, priority, callback);
                _pendingTaskMap.Add(bundlePath, waitTask);
                _pendingTaskQueue.Enqueue(waitTask);
            }
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
                    if (_pendingTaskMap.Remove(runningTask.Path, out var pendingTask))
                    {
                        LiteUtils.SafeInvoke(pendingTask.Callback, bundle);
                    }
                }
            }
            
            while (_runningTasks.Count < _concurrencyLimit && _pendingTaskQueue.Count > 0)
            {
                var info = _pendingTaskQueue.Dequeue();
                var task = _bundleLocater.LoadBundle(info.Path, null);
                task.SetPriority(TaskPriority.Urgent);
                _runningTasks.Add(new RunningTask(info.Path, task));
            }
        }
        
        private class PendingTask
        {
            public string Path { get; }
            public int Priority { get; set; }
            public Action<AssetBundle> Callback { get; set; }
            
            public PendingTask(string path, int priority, Action<AssetBundle> callback)
            {
                Path = path;
                Priority = priority;
                Callback = callback;
            }
        }
        
        private class RunningTask
        {
            public string Path { get; }
            public LoadBundleBaseTask LoadTask { get; }

            public RunningTask(string path, LoadBundleBaseTask loadTask)
            {
                Path = path;
                LoadTask = loadTask;
            }
        }
    }
}