#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
        private readonly HashSet<string> _loadingSceneSet = new(StringComparer.OrdinalIgnoreCase);

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
            _loadingSceneSet.Clear();
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

        public void LoadAssetAsync<T>(string assetPath, Action<AssetInfoCache.Lease<T>> callback) where T : UnityEngine.Object
        {
            SimulateAsync(callback, LoadAssetSync<T>(assetPath));
        }

        public AssetInfoCache.Lease<T> LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object
        {
            var fullPath = PathUtils.GetFullPathInAssetRoot(assetPath);
            var asset = AssetDatabase.LoadAssetAtPath<T>(fullPath);
            if (asset == null)
            {
                LLog.Error("Can't load asset : {0}", fullPath);
                return null;
            }
            return AssetInfoCache.Lease<T>.CreateUncached(asset);
        }

        public void LoadSceneAsync(string scenePath, LoadSceneParameters parameters, Action<SceneInfoCache.Lease> callback)
        {
            var sceneName = PathUtils.GetFileNameWithoutExt(scenePath);
            if (!_loadingSceneSet.Add(sceneName))
            {
                LLog.Error("Scene is already loading: {0}", sceneName);
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            LoadSceneInternal().Forget();

            async UniTask LoadSceneInternal()
            {
                try
                {
                    var fullPath = PathUtils.GetFullPathInAssetRoot(scenePath);
                    var loadScene = EditorSceneManager.LoadSceneInPlayMode(fullPath, parameters);
                    await UniTask.WaitUntil(() => !loadScene.IsValid() || loadScene.isLoaded);

                    SimulateAsync(scene =>
                    {
                        if (!scene.IsValid() || !scene.isLoaded)
                        {
                            LLog.Error("Can't load scene : {0}", fullPath);
                            _loadingSceneSet.Remove(sceneName);
                            LiteUtils.SafeInvoke(callback, null);
                            return;
                        }

                        var cache = new SceneInfoCache(null, sceneName, parameters);
                        cache.LoadSceneForEditor(scene, lease =>
                        {
                            _loadingSceneSet.Remove(sceneName);

                            if (!scene.IsValid() || !scene.isLoaded)
                            {
                                lease?.Dispose();
                                LiteUtils.SafeInvoke(callback, null);
                                return;
                            }

                            LiteUtils.SafeInvoke(callback, lease);
                        });
                    }, loadScene);
                }
                catch (Exception ex)
                {
                    _loadingSceneSet.Remove(sceneName);
                    LLog.Exception(ex, "Load scene failed : {0}", scenePath);
                    LiteUtils.SafeInvoke(callback, null);
                }
            }
        }

        public void UnloadUnusedAssets(int maxDepth)
        {
            UnityEngine.Resources.UnloadUnusedAssets();
            GC.Collect();
        }
    }
}
#endif
