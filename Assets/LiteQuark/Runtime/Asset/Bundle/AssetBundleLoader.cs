using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal class AssetBundleLoader
    {
        private readonly IBundleLocater _bundleLocater;
        private readonly int _concurrencyLimit;
        
        private readonly Dictionary<string, WaitingTask> _waitTaskMap;
        private readonly PriorityQueue<WaitingTask> _waitTaskQueue;
        private readonly List<RunningTask> _runningTasks;
        
        public AssetBundleLoader(IBundleLocater locater, int concurrencyLimit)
        {
            _bundleLocater = locater;
            _concurrencyLimit = concurrencyLimit;
            
            _waitTaskMap = new Dictionary<string, WaitingTask>();
            _waitTaskQueue = new PriorityQueue<WaitingTask>((x, y) => x.Priority.CompareTo(y.Priority));
            _runningTasks = new List<RunningTask>();
        }

        public void LoadBundle(string bundlePath, int priority, Action<AssetBundle> callback)
        {
            if (_waitTaskMap.TryGetValue(bundlePath, out var loadInfo))
            {
                loadInfo.Priority = Mathf.Max(loadInfo.Priority, priority);
                loadInfo.Callback += callback;
            }
            else
            {
                loadInfo = new WaitingTask(bundlePath, callback, priority);
                _waitTaskMap.Add(bundlePath, loadInfo);
                _waitTaskQueue.Enqueue(loadInfo);
            }
        }

        public void Post()
        {
            for (var i = _runningTasks.Count - 1; i >= 0; i--)
            {
                var running = _runningTasks[i];
                if (running.Task.IsDone)
                {
                    _runningTasks.RemoveAt(i);

                    var bundle = running.Task.GetBundle();
                    if (_waitTaskMap.Remove(running.Path, out var info))
                    {
                        try { info.Callback?.Invoke(bundle); }
                        catch (Exception ex) { LLog.Exception(ex); }
                    }
                }
            }
            
            while (_runningTasks.Count < _concurrencyLimit && _waitTaskQueue.Count > 0)
            {
                var info = _waitTaskQueue.Dequeue();
                var task = _bundleLocater.LoadBundle(info.Path, null);
                _runningTasks.Add(new RunningTask(info.Path, task));
            }
        }
        
        private class WaitingTask
        {
            public string Path { get; }
            public Action<AssetBundle> Callback { get; set; }
            public int Priority { get; set; }
            
            public WaitingTask(string path, Action<AssetBundle> callback, int priority)
            {
                Path = path;
                Callback = callback;
                Priority = priority;
            }
        }
        
        private class RunningTask
        {
            public string Path { get; }
            public LoadBundleBaseTask Task { get; }

            public RunningTask(string path, LoadBundleBaseTask task)
            {
                Path = path;
                Task = task;
            }
        }
    }
}