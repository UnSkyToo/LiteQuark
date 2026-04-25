#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    internal sealed class AssetEditorProvider : IAssetProvider
    {
        private bool _simulateAsyncDelayInEditor;
        private int _asyncDelayMinFrame;
        private int _asyncDelayMaxFrame;
        
        public AssetEditorProvider()
        {
        }

        public UniTask<bool> Initialize()
        {
            if (LiteRuntime.Setting.Asset.SimulateAsyncDelayInEditor)
            {
                _simulateAsyncDelayInEditor = true;
                _asyncDelayMinFrame = Math.Max(0, LiteRuntime.Setting.Asset.AsyncDelayMinFrame);
                _asyncDelayMaxFrame = Math.Min(60, LiteRuntime.Setting.Asset.AsyncDelayMaxFrame);
            }
            else
            {
                _simulateAsyncDelayInEditor = false;
            }
            
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

        private void SimulateAsync<T>(Action<T> callback, T value)
        {
            if (_simulateAsyncDelayInEditor)
            {
                LiteRuntime.Timer.AddTimerWithFrame(UnityEngine.Random.Range(_asyncDelayMinFrame, _asyncDelayMaxFrame), () =>
                {
                    LiteUtils.SafeInvoke(callback, value);
                });
            }
            else
            {
                LiteRuntime.Timer.NextFrame(() => { LiteUtils.SafeInvoke(callback, value); });
            }
        }
        
        public bool HasAsset(string assetPath)
        {
            var fullPath = PathUtils.GetFullPathInAssetRoot(assetPath);
            return AssetDatabase.GetMainAssetTypeAtPath(fullPath) != null;
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
                LLog.Error("Can't load asset : {0}", fullPath);
            }
            return asset;
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
                return true;
            }
            
            return EditorSceneManager.LoadSceneInPlayMode(fullPath, parameters).isLoaded;
        }

        public void UnloadAsset(string assetPath)
        {
        }

        public void UnloadSceneAsync(string scenePath, string sceneName, Action callback)
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
            UnityEngine.Resources.UnloadUnusedAssets();
            GC.Collect();
        }
    }
}
#endif
