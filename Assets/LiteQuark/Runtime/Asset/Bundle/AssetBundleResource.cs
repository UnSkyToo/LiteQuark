using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal sealed class AssetBundleResource : IBundleResource
    {
        private AssetBundle _bundle;

        public AssetBundleResource(AssetBundle bundle)
        {
            _bundle = bundle != null ? bundle : throw new ArgumentNullException(nameof(bundle));
        }

        public string[] GetAllScenePaths()
        {
            ThrowIfDisposed();
            return _bundle.GetAllScenePaths();
        }

        public BaseTask CreateLoadAssetTask<T>(string assetKey, Action<ITask> callback) where T : UnityEngine.Object
        {
            ThrowIfDisposed();
            return new LoadAssetTask<T>(_bundle, assetKey, callback);
        }

        public long GetRuntimeMemorySize()
        {
            ThrowIfDisposed();
            return UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(_bundle);
        }

        public void Dispose()
        {
            if (_bundle != null)
            {
                _bundle.Unload(true);
            }
            _bundle = null;
        }

        private void ThrowIfDisposed()
        {
            if (_bundle == null)
            {
                throw new ObjectDisposedException(nameof(AssetBundleResource));
            }
        }
    }
}
