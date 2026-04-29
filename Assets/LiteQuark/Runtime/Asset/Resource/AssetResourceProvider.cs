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

        public void LoadAssetAsync<T>(string assetPath, System.Action<T> callback) where T : Object
        {
            LiteRuntime.Task.AddLoadResourceTask(assetPath, callback);
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

        public void ReleaseAssetReference(string assetPath)
        {
        }

        public void ReleaseSceneReferenceAsync(string scenePath, string sceneName, System.Action callback)
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
