using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    internal interface IAssetManager : ITick, IDisposeAsync
    {
        UniTask<bool> Initialize(IBundleProvider bundleProvider);

        string GetVersion();

        bool HasAsset(string assetKey);

        void LoadAssetAsync<T>(string assetKey, AssetLoadPriority priority, Action<AssetLoadResult<IAssetLease<T>>> callback) where T : UnityEngine.Object;
        void LoadSceneAsync(string sceneKey, LoadSceneParameters parameters, AssetLoadPriority priority, Action<AssetLoadResult<IAssetLease<Scene?>>> callback);

        void UnloadUnusedAssets(int maxDepth);
    }
}
