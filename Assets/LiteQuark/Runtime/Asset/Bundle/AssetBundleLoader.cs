using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal class AssetBundleLoader
    {
        private readonly IBundleLocater _bundleLocater;
        private readonly int _concurrencyLimit;
        
        private readonly Dictionary<string, InternalLoadTask> _loadMap;
        private readonly PriorityQueue<InternalLoadTask> _loadQueues;
        private readonly Dictionary<string, LoadBundleBaseTask> _loadTasks;
        private readonly List<string> _loadCompleteList;
        
        public AssetBundleLoader(IBundleLocater locater, int concurrencyLimit)
        {
            _bundleLocater = locater;
            _concurrencyLimit = concurrencyLimit;
            
            _loadMap = new Dictionary<string, InternalLoadTask>();
            _loadQueues = new PriorityQueue<InternalLoadTask>((x, y) => x.Priority.CompareTo(y.Priority));
            _loadTasks = new Dictionary<string, LoadBundleBaseTask>();
            _loadCompleteList = new List<string>();
        }

        public void LoadBundle(string bundlePath, int priority, Action<AssetBundle> callback)
        {
            if (_loadMap.TryGetValue(bundlePath, out var loadInfo))
            {
                loadInfo.Priority = Mathf.Max(loadInfo.Priority, priority);
                loadInfo.Callback += callback;
            }
            else
            {
                loadInfo = new InternalLoadTask(bundlePath, callback, priority);
                _loadMap.Add(bundlePath, loadInfo);
                _loadQueues.Enqueue(loadInfo);
            }
        }

        public void Post()
        {
            foreach (var (path, task) in _loadTasks)
            {
                if (task.IsDone)
                {
                    _loadCompleteList.Add(path);
                }
            }

            if (_loadCompleteList.Count > 0)
            {
                foreach (var path in _loadCompleteList)
                {
                    var bundle = _loadTasks[path]?.GetBundle();
                    _loadTasks.Remove(path);
                    
                    if (_loadMap.Remove(path, out var info))
                    {
                        _loadQueues.Remove(info);
                        
                        try { info.Callback?.Invoke(bundle); }
                        catch (Exception ex) { LLog.Exception(ex); }
                    }
                }
                
                _loadCompleteList.Clear();
            }
            
            if (_loadTasks.Count < _concurrencyLimit)
            {
                while (_loadTasks.Count < _concurrencyLimit && _loadQueues.Count > 0)
                {
                    var info = _loadQueues.Dequeue();
                    _loadTasks.Add(info.Path, _bundleLocater.LoadBundle(info.Path, null));
                }
            }
        }
        
        private class InternalLoadTask
        {
            public string Path { get; }
            public Action<AssetBundle> Callback { get; set; }
            public int Priority { get; set; }
            
            public InternalLoadTask(string path, Action<AssetBundle> callback, int priority)
            {
                Path = path;
                Callback = callback;
                Priority = priority;
            }
        }
    }
}