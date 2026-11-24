using System.Linq;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache
    {
        internal BundleVisitorInfo GetVisitorInfo()
        {
            var memSize = IsLoaded ? Bundle.GetRuntimeMemorySize() : 0L;
            var info = new BundleVisitorInfo(_bundleInfo.BundlePath,
                memSize,
                _refCount,
                Stage.ToString(),
                _retainTime,
                _sceneCacheMap?.Values.Where(scene => scene.Stage is not AssetCacheStage.Idle and not AssetCacheStage.Unloaded)
                    .Select(scene => scene.GetVisitorInfo()).ToArray() ?? System.Array.Empty<SceneVisitorInfo>(),
                _assetCacheMap.Select(chunk => chunk.Value.GetVisitorInfo()).ToArray());
            return info;
        }
    }
}
