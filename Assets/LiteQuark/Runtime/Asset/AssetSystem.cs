﻿using System;
using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class AssetSystem : ISystem, ITick
    {
        private IAssetLoader Loader_ = null;

        public AssetSystem()
        {
#if UNITY_EDITOR
            var mode = LiteRuntime.Setting.Asset.AssetMode;
#else
            var mode = AssetLoaderMode.Bundle;
#endif
            switch (mode)
            {
#if UNITY_EDITOR
                case AssetLoaderMode.Internal:
                    Loader_ = new AssetDatabaseLoader();
                    break;
#endif
                case AssetLoaderMode.Bundle:
                    Loader_ = new AssetBundleLoader();
                    break;
                default:
                    throw new ArgumentException($"error {nameof(AssetLoaderMode)} : {mode}");
            }
        }
        
        public Task<bool> Initialize()
        {
            return Loader_.Initialize();
        }

        public void Dispose()
        {
            Loader_?.UnloadUnusedAssets(20);
            
            Loader_?.Dispose();
            Loader_ = null;
        }

        public void Tick(float deltaTime)
        {
            Loader_?.Tick(deltaTime);
        }

        private string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            
            return path.TrimStart('/').ToLower();
        }

        public void PreloadBundle(string bundlePath, Action<bool> callback)
        {
            var formatPath = FormatPath(bundlePath);
            Loader_?.PreloadBundle(formatPath, callback);
        }

        public Task<bool> PreloadBundle(string bundlePath)
        {
            var tcs = new TaskCompletionSource<bool>();
            PreloadBundle(bundlePath, (result) =>
            {
                tcs.SetResult(result);
            });
            return tcs.Task;
        }

        public void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            Loader_?.PreloadAsset<T>(formatPath, callback);
        }

        public Task<bool> PreloadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            var tcs = new TaskCompletionSource<bool>();
            PreloadAsset<T>(assetPath, (result) =>
            {
                tcs.SetResult(result);
            });
            return tcs.Task;
        }

        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            Loader_?.LoadAssetAsync<T>(formatPath, callback);
        }
        
        public Task<T> LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            var tcs = new TaskCompletionSource<T>();
            LoadAssetAsync<T>(assetPath, (asset) =>
            {
                tcs.SetResult(asset);
            });
            return tcs.Task;
        }

        public T LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            return Loader_?.LoadAssetSync<T>(formatPath);
        }

        public void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback)
        {
            var formatPath = FormatPath(assetPath);
            Loader_?.InstantiateAsync(formatPath, parent, callback);
        }
        
        public Task<UnityEngine.GameObject> InstantiateAsync(string assetPath, UnityEngine.Transform parent)
        {
            var tcs = new TaskCompletionSource<UnityEngine.GameObject>();
            InstantiateAsync(assetPath, parent, (gameObject) =>
            {
                tcs.SetResult(gameObject);
            });
            return tcs.Task;
        }

        public UnityEngine.GameObject InstantiateSync(string assetPath, UnityEngine.Transform parent)
        {
            var formatPath = FormatPath(assetPath);
            return Loader_?.InstantiateSync(formatPath, parent);
        }
        
        public void LoadSceneAsync(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters, Action<bool> callback)
        {
            var sceneName = PathUtils.GetFileNameWithoutExt(scenePath);
            var formatPath = FormatPath(scenePath);
            Loader_?.LoadSceneAsync(formatPath, sceneName, parameters, callback);
        }
        
        public Task<bool> LoadSceneAsync(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters)
        {
            var tcs = new TaskCompletionSource<bool>();
            LoadSceneAsync(scenePath, parameters, (result) =>
            {
                tcs.SetResult(result);
            });
            return tcs.Task;
        }
        
        public bool LoadSceneSync(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters)
        {
            var sceneName = PathUtils.GetFileNameWithoutExt(scenePath);
            var formatPath = FormatPath(scenePath);
            return Loader_?.LoadSceneSync(formatPath, sceneName, parameters) ?? false;
        }

        public void UnloadAsset(string assetPath)
        {
            var formatPath = FormatPath(assetPath);
            Loader_?.UnloadAsset(formatPath);
        }

        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object
        {
            Loader_?.UnloadAsset(asset);
        }
        
        public void UnloadSceneAsync(string scenePath, Action callback)
        {
            var formatPath = FormatPath(scenePath);
            Loader_?.UnloadSceneAsync(formatPath, callback);
        }

        /// <summary>
        /// 释放未使用的资源（包括处于Retain的缓存资源），可以在需要的时候调用
        /// </summary>
        /// <param name="maxDepth">循环释放嵌套引用的最大层数。例如：A->B->C，如果为2，则只释放到B这一层</param>
        public void UnloadUnusedAssets(int maxDepth = 5)
        {
            Loader_?.UnloadUnusedAssets(maxDepth);
        }
        
#if UNITY_EDITOR
        public VisitorInfo GetVisitorInfo()
        {
            if (Loader_ is AssetBundleLoader loader)
            {
                return loader.GetVisitorInfo();
            }

            return new VisitorInfo(null);
        }
#endif
    }
}