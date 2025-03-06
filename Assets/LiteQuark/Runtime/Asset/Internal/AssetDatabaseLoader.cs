#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEditor;

namespace LiteQuark.Runtime
{
    internal sealed class AssetDatabaseLoader : IAssetLoader
    {
        private bool SimulateAsyncDelayInEditor_;
        private float AsyncDelayMinTime_;
        private float AsyncDelayMaxTime_;
        
        public AssetDatabaseLoader()
        {
        }

        public Task<bool> Initialize()
        {
            if (LiteRuntime.Setting.Asset.SimulateAsyncDelayInEditor)
            {
                SimulateAsyncDelayInEditor_ = true;
                AsyncDelayMinTime_ = MathF.Max(0f, LiteRuntime.Setting.Asset.AsyncDelayMinTime);
                AsyncDelayMaxTime_ = MathF.Min(10f, LiteRuntime.Setting.Asset.AsyncDelayMaxTime);
            }
            else
            {
                SimulateAsyncDelayInEditor_ = false;
            }
            
            return Task.FromResult(true);
        }

        public void Dispose()
        {
        }

        public void Tick(float deltaTime)
        {
        }

        private void SimulateAsync<T>(Action<T> callback, T value)
        {
            if (SimulateAsyncDelayInEditor_)
            {
                LiteRuntime.Timer.AddTimer(UnityEngine.Random.Range(AsyncDelayMinTime_, AsyncDelayMaxTime_), () =>
                {
                    callback?.Invoke(value);
                });
            }
            else
            {
                callback?.Invoke(value);
            }
        }

        public void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object
        {
            SimulateAsync(callback, true);
        }

        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            SimulateAsync(callback, LoadAssetSync<T>(assetPath));
        }

        public T LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object
        {
            var fullPath = PathUtils.GetFullPathInAssetRoot(assetPath);
            var asset = AssetDatabase.LoadAssetAtPath<T>(fullPath);
            if (asset == null)
            {
                LLog.Error($"can't load asset : {fullPath}");
            }
            return asset;
        }

        public void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback)
        {
            LoadAssetAsync<UnityEngine.GameObject>(assetPath, (asset) =>
            {
                var instance = UnityEngine.Object.Instantiate(asset, parent);
                callback?.Invoke(instance);
            });
        }

        public UnityEngine.GameObject InstantiateSync(string assetPath, UnityEngine.Transform parent)
        {
            var asset = LoadAssetSync<UnityEngine.GameObject>(assetPath);
            var instance = UnityEngine.Object.Instantiate(asset, parent);
            return instance;
        }

        public void UnloadAsset(string assetPath)
        {
        }

        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object
        {
            if (asset is UnityEngine.GameObject go)
            {
                if (go.scene.isLoaded)
                {
                    UnityEngine.Object.DestroyImmediate(asset);
                }
            }
        }

        public void UnloadUnusedAssets()
        {
            UnityEngine.Resources.UnloadUnusedAssets();
        }
    }
}
#endif