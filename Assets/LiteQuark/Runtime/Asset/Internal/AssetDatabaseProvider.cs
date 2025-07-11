#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    internal sealed class AssetDatabaseProvider : IAssetProvider
    {
        private bool _simulateAsyncDelayInEditor;
        private float _asyncDelayMinTime;
        private float _asyncDelayMaxTime;
        
        public AssetDatabaseProvider()
        {
        }

        public Task<bool> Initialize()
        {
            if (LiteRuntime.Setting.Asset.SimulateAsyncDelayInEditor)
            {
                _simulateAsyncDelayInEditor = true;
                _asyncDelayMinTime = MathF.Max(0f, LiteRuntime.Setting.Asset.AsyncDelayMinTime);
                _asyncDelayMaxTime = MathF.Min(10f, LiteRuntime.Setting.Asset.AsyncDelayMaxTime);
            }
            else
            {
                _simulateAsyncDelayInEditor = false;
            }
            
            return Task.FromResult(true);
        }

        public void Dispose()
        {
        }

        public void Tick(float deltaTime)
        {
        }

        public string GetVersion()
        {
            return AppUtils.GetVersion();
        }

        private void SimulateAsync<T>(Action<T> callback, T value)
        {
            if (_simulateAsyncDelayInEditor)
            {
                LiteRuntime.Timer.AddTimer(UnityEngine.Random.Range(_asyncDelayMinTime, _asyncDelayMaxTime), () =>
                {
                    callback?.Invoke(value);
                });
            }
            else
            {
                LiteRuntime.Timer.NextFrame(() => { callback?.Invoke(value); });
            }
        }

        public void PreloadBundle(string bundlePath, Action<bool> callback)
        {
            SimulateAsync(callback, true);
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
        
        public void LoadSceneAsync(string scenePath, string sceneName, LoadSceneParameters parameters, Action<bool> callback)
        {
            SimulateAsync(callback, LoadSceneSync(scenePath, sceneName, parameters));
        }
        
        public bool LoadSceneSync(string scenePath, string sceneName, LoadSceneParameters parameters)
        {
            var fullPath = PathUtils.GetFullPathInAssetRoot(scenePath);
            if (SceneManager.GetSceneByPath(fullPath).isLoaded)
            {
                return false;
            }
            
            return EditorSceneManager.LoadSceneInPlayMode(fullPath, parameters).isLoaded;
        }

        public void UnloadAsset(string assetPath)
        {
        }

        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object
        {
            if (asset == null)
            {
                return;
            }
            
            if (asset is UnityEngine.GameObject go)
            {
                if (go.scene.isLoaded)
                {
                    UnityEngine.Object.Destroy(asset);
                }
            }
        }

        public void UnloadSceneAsync(string scenePath, Action callback)
        {
            var sceneName = PathUtils.GetFileNameWithoutExt(scenePath);
            var op = SceneManager.UnloadSceneAsync(sceneName);
            if (op == null)
            {
                callback?.Invoke();
                return;
            }
            
            op.completed += (result) =>
            {
                callback?.Invoke();
            };
        }

        public void UnloadUnusedAssets(int maxDepth)
        {
            UnityEngine.Resources.UnloadUnusedAssets();
            GC.Collect();
        }
    }
}
#endif