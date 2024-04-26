using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class ConfigSystem : ISystem
    {
        private readonly Dictionary<Type, Dictionary<int, IJsonMainConfig>> ConfigCache = new Dictionary<Type, Dictionary<int, IJsonMainConfig>>();

        
        public ConfigSystem()
        {
        }
        
        public void Dispose()
        {
        }
        
        public T[] GetDataList<T>() where T : IJsonMainConfig
        {
            var type = typeof(T);
            if (ConfigCache.TryGetValue(type, out var cache))
            {
                return cache.Values.Cast<T>().ToArray();
            }

            LLog.Info($"error config type : {type}");
            return Array.Empty<T>();
        }

        public T GetData<T>(int id) where T : IJsonMainConfig
        {
            var type = typeof(T);
            if (ConfigCache.TryGetValue(type, out var cache))
            {
                if (cache.TryGetValue(id, out var data) && data is T tVal)
                {
                    return tVal;
                }
                else
                {
                    LLog.Info($"error {typeof(T).Name} config id : {id}");
                }
            }
            else
            {
                LLog.Info($"error config type : {type}");
            }

            return default;
        }
        
        public void LoadFromJson(Dictionary<Type, string> configs)
        {
            foreach (var config in configs)
            {
                var cache = new Dictionary<int, IJsonMainConfig>();
                ConfigCache.Add(config.Key, cache);
                LoadConfigTable(config.Value, config.Key, cache);
            }
        }
        
        private void LoadConfigTable(string jsonFilePath, Type type, Dictionary<int, IJsonMainConfig> cache)
        {
            LiteRuntime.Asset.LoadAssetAsync<TextAsset>(jsonFilePath, (asset) =>
            {
                var data =  JsonUtils.DecodeArray(asset.text, type).Cast<IJsonMainConfig>().ToArray();
                if (data == null)
                {
                    LLog.Info($"read json file error : {jsonFilePath}");
                    return;
                }
            
                LLog.Info($"load config : {jsonFilePath}");
                foreach (var item in data)
                {
                    cache.Add(item.ID, item);
                }
            });
        }
    }
}