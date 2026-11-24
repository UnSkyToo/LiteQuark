#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteQuark.Runtime
{
    internal sealed class SimulateBundleResource : IBundleResource
    {
        private Dictionary<string, string> _sourcePaths = new(StringComparer.Ordinal);

        public SimulateBundleResource(BundleInfo bundleInfo, IReadOnlyDictionary<string, string> sourcePaths)
        {
            foreach (var assetKey in bundleInfo.AssetList)
            {
                _sourcePaths.Add(assetKey, sourcePaths[assetKey]);
            }
        }

        public string[] GetAllScenePaths()
        {
            ThrowIfDisposed();
            return _sourcePaths.Values.Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        public long GetRuntimeMemorySize()
        {
            ThrowIfDisposed();
            return 0;
        }

        public BaseTask CreateLoadAssetTask<T>(string assetKey, Action<ITask> callback) where T : UnityEngine.Object
        {
            ThrowIfDisposed();
            return new LoadSimulateAssetTask<T>(_sourcePaths[assetKey], callback);
        }

        public BaseTask CreateLoadSceneTask(string loadPath, Action<LoadSceneTask> callback)
        {
            ThrowIfDisposed();
            return new LoadEditorSceneTask(loadPath, callback);
        }

        public void Dispose()
        {
            // AssetDatabase 对象由编辑器共享；释放容器不销毁项目资源。
            _sourcePaths = null;
        }

        private void ThrowIfDisposed()
        {
            if (_sourcePaths == null)
            {
                throw new ObjectDisposedException(nameof(SimulateBundleResource));
            }
        }
    }
}
#endif
