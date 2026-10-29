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
        private readonly List<SceneInfoCache> _sceneCaches = new();
        private int _pendingDeliveries;
        private bool _isDisposed;

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

        public async UniTask DisposeAsync()
        {
            _isDisposed = true;
            while (_pendingDeliveries > 0 || _loadingSceneSet.Count > 0)
            {
                await UniTask.NextFrame();
            }
            var cleanups = new List<UniTask>(_sceneCaches.Count);
            foreach (var scene in _sceneCaches)
            {
                cleanups.Add(scene.DisposeAsync());
            }
            await UniTaskUtils.WhenAllCompleteAsync(cleanups);
            _sceneCaches.Clear();
            _loadingSceneSet.Clear();
        }

        public void Tick(float deltaTime)
        {
            _sceneCaches.RemoveAll(static scene => scene.Stage == AssetCacheStage.Unloaded);
        }

        public string GetVersion()
        {
            return AppUtils.GetVersion();
        }

        private void SimulateAsync<T>(Action<T> callback, T value)
        {
            _pendingDeliveries++;
            void Deliver()
            {
                try
                {
                    LiteUtils.SafeInvoke(callback, value);
                }
                finally
                {
                    _pendingDeliveries--;
                }
            }
            if (_simulateAsyncDelayInEditor)
            {
                LiteRuntime.Timer.AddTimerWithFrame(UnityEngine.Random.Range(_asyncDelayMinFrame, _asyncDelayMaxFrame), Deliver);
            }
            else
            {
                LiteRuntime.Timer.NextFrame(Deliver);
            }
        }

        public bool HasAsset(string assetKey)
        {
            return AssetDatabase.GetMainAssetTypeAtPath(assetKey) != null;
        }

        public void LoadAssetAsync<T>(string assetKey, AssetLoadPriority priority, Action<AssetLoadResult<IAssetLease<T>>> callback) where T : UnityEngine.Object
        {
            if (_isDisposed)
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<T>>.Failure(new AssetError(assetKey, "Asset provider is closing or disposed")));
                return;
            }
            var result = LoadAssetSync<T>(assetKey);
            SimulateAsync(delivery =>
            {
                if (_isDisposed)
                {
                    delivery.Value?.Dispose();
                    delivery = AssetLoadResult<IAssetLease<T>>.Failure(new AssetError(assetKey, "Asset provider is closing"));
                }
                LiteUtils.SafeInvoke(callback, delivery);
            }, result);
        }

        private AssetLoadResult<IAssetLease<T>> LoadAssetSync<T>(string assetKey) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetKey);
            if (asset == null)
            {
                LLog.Error("Can't load asset : {0}", assetKey);
                return AssetLoadResult<IAssetLease<T>>.Failure(new AssetError(assetKey, $"Editor asset load returned no {typeof(T).Name}"));
            }
            return AssetLoadResult<IAssetLease<T>>.Success(AssetInfoCache.Lease<T>.CreateUncached(asset));
        }

        public void LoadSceneAsync(string sceneKey, LoadSceneParameters parameters, AssetLoadPriority priority, Action<AssetLoadResult<IAssetLease<Scene?>>> callback)
        {
            if (_isDisposed)
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<Scene?>>.Failure(new AssetError(sceneKey, "Asset provider is closing or disposed")));
                return;
            }
            if (!_loadingSceneSet.Add(sceneKey))
            {
                LLog.Error("Scene is already loading: {0}", sceneKey);
                LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<Scene?>>.Failure(new AssetError(sceneKey, "Scene is already loading")));
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
                        LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<Scene?>>.Failure(new AssetError(sceneKey, "Editor scene asset was not found")));
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
                            LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<Scene?>>.Failure(new AssetError(sceneKey, "Editor scene is no longer loaded")));
                            return;
                        }

                        var cache = new SceneInfoCache(null, sceneKey, loadPath, parameters);
                        _sceneCaches.Add(cache);
                        cache.LoadSceneForEditor(scene, result =>
                        {
                            _loadingSceneSet.Remove(sceneKey);

                            if (_isDisposed)
                            {
                                result.Value?.Dispose();
                                LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<Scene?>>.Failure(new AssetError(sceneKey, "Asset provider is closing")));
                                return;
                            }

                            if (!scene.IsValid() || !scene.isLoaded)
                            {
                                result.Value?.Dispose();
                                LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<Scene?>>.Failure(result.Error ?? new AssetError(sceneKey, "Editor scene is no longer loaded")));
                                return;
                            }

                            LiteUtils.SafeInvoke(callback, result);
                        });
                    }, loadScene);
                }
                catch (Exception ex)
                {
                    _loadingSceneSet.Remove(sceneKey);
                    LLog.Exception(ex, "Load scene failed : {0}", sceneKey);
                    LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<Scene?>>.Failure(new AssetError(sceneKey, ex.Message, ex)));
                }
            }
        }

        public void UnloadUnusedAssets(int maxDepth)
        {
            if (_isDisposed)
            {
                return;
            }
            UnityEngine.Resources.UnloadUnusedAssets();
            GC.Collect();
        }
    }
}
#endif
