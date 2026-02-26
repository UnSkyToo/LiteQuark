using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    internal sealed class AssetResourceProvider : IAssetProvider
    {
        public AssetResourceProvider()
        {
        }
        
        public UniTask<bool> Initialize()
        {
            return UniTask.FromResult(true);
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
        
        public bool HasAsset(string assetPath)
        {
            return Resources.Load(assetPath) != null;
        }

        public void PreloadAsset<T>(string assetPath, System.Action<bool> callback) where T : Object
        {
            LiteUtils.SafeInvoke(callback, true);
        }

        public void LoadAssetAsync<T>(string assetPath, System.Action<T> callback) where T : Object
        {
            LiteRuntime.Task.AddLoadResourceTask(assetPath, callback);
        }

        public void InstantiateAsync(string assetPath, Transform parent, System.Action<GameObject> callback)
        {
            LoadAssetAsync<GameObject>(assetPath, (asset) =>
            {
                if (asset == null)
                {
                    LiteUtils.SafeInvoke(callback, null);
                    return;
                }
                
                var instance = Object.Instantiate(asset, parent);
                LiteUtils.SafeInvoke(callback, instance);
            });
        }

        public void InstantiateAsync(string assetPath, Transform parent, Vector3 position, Quaternion rotation, System.Action<GameObject> callback)
        {
            LoadAssetAsync<GameObject>(assetPath, (asset) =>
            {
                if (asset == null)
                {
                    LiteUtils.SafeInvoke(callback, null);
                    return;
                }
                
                var instance = Object.Instantiate(asset, position, rotation, parent);
                LiteUtils.SafeInvoke(callback, instance);
            });
        }

        public void LoadSceneAsync(string scenePath, string sceneName, LoadSceneParameters parameters, System.Action<bool> callback)
        {
            var fullPath = PathUtils.GetFullPathInAssetRoot(scenePath);
            if (SceneManager.GetSceneByPath(fullPath).isLoaded)
            {
                LiteUtils.SafeInvoke(callback, true);
                return;
            }
            
            LiteRuntime.Task.AddLoadSceneTask(sceneName, parameters, callback);
        }

        public void UnloadAsset(string assetPath)
        {
        }

        public void UnloadAsset<T>(T asset) where T : Object
        {
            if (asset == null)
            {
                return;
            }

            if (asset is GameObject go)
            {
                if (go.scene.isLoaded)
                {
                    Object.Destroy(asset);
                }
            }
        }

        public void UnloadSceneAsync(string scenePath, string sceneName, System.Action callback)
        {
            var op = SceneManager.UnloadSceneAsync(sceneName);
            if (op == null)
            {
                LiteUtils.SafeInvoke(callback);
                return;
            }
            
            op.completed += (result) =>
            {
                LiteUtils.SafeInvoke(callback);
            };
        }

        public void UnloadUnusedAssets(int maxDepth)
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
    }
}