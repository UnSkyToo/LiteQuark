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

        // 共享请求由 Cache 合并，只有尚未提交的 Bundle 需要按逻辑路径查找并提权。
        private readonly Dictionary<string, BundleRequest> _pendingByPath = new();
        private readonly PriorityQueue<BundleRequest> _pendingQueue = new(static (x, y) => ((int)x.Priority).CompareTo((int)y.Priority));
        private readonly List<BundleRequest> _submittedRequests = new();
        private bool _isDisposed;

        public AssetBundleLoader(IBundleProvider provider, VersionPackInfo packInfo, int concurrencyLimit)
        {
            _bundleProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            _packInfo = packInfo ?? throw new ArgumentNullException(nameof(packInfo));
            _concurrencyLimit = Mathf.Max(1, concurrencyLimit);
        }

        public async UniTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            _pendingByPath.Clear();
            _pendingQueue.Clear();

            var stops = new UniTask[_submittedRequests.Count];
            for (var i = 0; i < stops.Length; i++)
            {
                stops[i] = StopAndReleaseAsync(_submittedRequests[i].LoadTask);
            }
            _submittedRequests.Clear();
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

            var loadPath = _packInfo.GetBundleFileLoadPath(bundleInfo);

            if (string.IsNullOrEmpty(loadPath))
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<IBundleResource>.Failure(new AssetError(bundleInfo.BundlePath, "Bundle load path is missing")));
                return;
            }

            var request = new BundleRequest(loadPath, bundleInfo.BundlePath, bundleInfo.Hash, priority, callback);
            _pendingByPath.Add(request.BundlePath, request);
            _pendingQueue.Enqueue(request);
        }

        public void RaiseBundlePriority(BundleInfo bundleInfo, AssetLoadPriority priority)
        {
            if (_pendingByPath.TryGetValue(bundleInfo.BundlePath, out var request) && priority > request.Priority)
            {
                request.Priority = priority;
                _pendingQueue.UpdatePriority(request);
            }
        }

        internal float GetProgress(string bundlePath)
        {
            foreach (var request in _submittedRequests)
            {
                if (request.BundlePath == bundlePath)
                {
                    return Mathf.Clamp01(request.LoadTask.Progress);
                }
            }
            return 0f;
        }

        public void Tick(float deltaTime)
        {
            if (_isDisposed)
            {
                return;
            }
            for (var i = _submittedRequests.Count - 1; i >= 0 && !_isDisposed; i--)
            {
                var request = _submittedRequests[i];
                var task = request.LoadTask;
                if (!task.IsDone)
                {
                    continue;
                }

                // 交付前移除记录，回调可以在当前栈上关闭 Loader。
                _submittedRequests.RemoveAt(i);
                request.Complete(task.GetBundle(), task.Error);
            }

            while (!_isDisposed && _submittedRequests.Count < _concurrencyLimit && _pendingQueue.Count > 0)
            {
                var request = _pendingQueue.Dequeue();
                _pendingByPath.Remove(request.BundlePath);

                try
                {
                    request.LoadTask = _bundleProvider.LoadBundle(request.LoadPath, request.Hash);
                }
                catch (Exception ex)
                {
                    LLog.Exception(ex, "Create bundle load task failed : {0}", request.LoadPath);
                    request.Complete(null, ex);
                    break;
                }

                _submittedRequests.Add(request);
            }
        }

        private sealed class BundleRequest
        {
            public string LoadPath { get; }
            public string BundlePath { get; }
            public string Hash { get; }
            public AssetLoadPriority Priority { get; set; }
            public ILoadBundleTask LoadTask { get; set; }

            private readonly Action<AssetLoadResult<IBundleResource>> _callback;

            public BundleRequest(string loadPath, string bundlePath, string hash, AssetLoadPriority priority, Action<AssetLoadResult<IBundleResource>> callback)
            {
                LoadPath = loadPath;
                BundlePath = bundlePath;
                Hash = hash;
                Priority = priority;
                _callback = callback;
            }

            public void Complete(IBundleResource bundle, Exception error)
            {
                var result = bundle != null
                    ? AssetLoadResult<IBundleResource>.Success(bundle)
                    : AssetLoadResult<IBundleResource>.Failure(new AssetError(BundlePath,
                        error?.Message ?? "Bundle load returned no result", error));
                LiteUtils.SafeInvoke(_callback, result);
            }
        }
    }
}
