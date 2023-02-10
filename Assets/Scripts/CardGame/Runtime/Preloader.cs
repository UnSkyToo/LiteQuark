using System;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteCard
{
    public sealed class Preloader : Singleton<Preloader>
    {
        public event Action<int, int> Progress;

        private int TotalCount_;
        private int CurrentCount_;

        public Preloader()
        {
        }

        public void Load()
        {
            TotalCount_ = GameConst.Preload.Count;
            CurrentCount_ = 0;

            void OnLoadCallback(bool isLoaded, string path)
            {
                if (!isLoaded)
                {
                    Log.Error($"preload asset error : {path}");
                    return;
                }
                
                CurrentCount_++;
                Progress?.Invoke(CurrentCount_, TotalCount_);
            }
            
            foreach (var prefabPath in GameConst.Preload.PrefabList)
            {
                LiteRuntime.Get<AssetSystem>().PreloadAsset<GameObject>(prefabPath, (isLoad) =>
                {
                    OnLoadCallback(isLoad, prefabPath);
                });
            }

            foreach (var jsonPath in GameConst.Preload.JsonList)
            {
                LiteRuntime.Get<AssetSystem>().PreloadAsset<TextAsset>(jsonPath, (isLoad) =>
                {
                    OnLoadCallback(isLoad, jsonPath);
                });
            }
        }

        public void Unload()
        {
            foreach (var prefabPath in GameConst.Preload.PrefabList)
            {
                LiteRuntime.Get<AssetSystem>().UnloadAsset(prefabPath);
            }

            foreach (var jsonPath in GameConst.Preload.JsonList)
            {
                LiteRuntime.Get<AssetSystem>().UnloadAsset(jsonPath);
            }
        }
    }
}