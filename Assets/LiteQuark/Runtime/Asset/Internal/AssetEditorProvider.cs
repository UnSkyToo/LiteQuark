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

        public bool HasAsset(string assetKey)
        {
            return AssetDatabase.GetMainAssetTypeAtPath(assetKey) != null;
        }

        public void LoadAssetAsync<T>(string assetKey, AssetLoadPriority priority, Action<AssetInfoCache.Lease<T>> callback) where T : UnityEngine.Object
        {
            SimulateAsync(callback, LoadAssetSync<T>(assetKey));
        }

        public AssetInfoCache.Lease<T> LoadAssetSync<T>(string assetKey) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetKey);
            if (asset == null)
            {
                LLog.Error("Can't load asset : {0}", assetKey);
                return null;
            }
            return AssetInfoCache.Lease<T>.CreateUncached(asset);
        }

        public void LoadSceneAsync(string sceneKey, LoadSceneParameters parameters, AssetLoadPriority priority, Action<SceneInfoCache.Lease> callback)
        {
            if (!_loadingSceneSet.Add(sceneKey))
            {
                LLog.Error("Scene is already loading: {0}", sceneKey);
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            LoadSceneInternal().Forget();

            async UniTask LoadSceneInternal()
            {
                try
                {
                    var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneKey);
                    if (sceneAsset == null)
                    {
                        _loadingSceneSet.Remove(sceneKey);
                        LiteUtils.SafeInvoke(callback, null);
                        return;
                    }

                    var loadPath = AssetDatabase.GetAssetPath(sceneAsset);
                    var loadScene = EditorSceneManager.LoadSceneInPlayMode(loadPath, parameters);
                    await UniTask.WaitUntil(() => !loadScene.IsValid() || loadScene.isLoaded);

                    SimulateAsync(scene =>
                    {
                        if (!scene.IsValid() || !scene.isLoaded)
                        {
                            LLog.Error("Can't load scene : {0}", loadPath);
                            _loadingSceneSet.Remove(sceneKey);
                            LiteUtils.SafeInvoke(callback, null);
                            return;
                        }

                        var cache = new SceneInfoCache(null, sceneKey, loadPath, parameters);
                        cache.LoadSceneForEditor(scene, lease =>
                        {
                            _loadingSceneSet.Remove(sceneKey);

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
                    _loadingSceneSet.Remove(sceneKey);
                    LLog.Exception(ex, "Load scene failed : {0}", sceneKey);
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
