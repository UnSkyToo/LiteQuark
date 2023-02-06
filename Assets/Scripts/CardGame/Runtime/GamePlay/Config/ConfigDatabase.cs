using System;
using System.Collections.Generic;
using System.Linq;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteCard.GamePlay
{
    public sealed class ConfigDatabase : Singleton<ConfigDatabase>
    {
        private readonly Dictionary<Type, Dictionary<int, IJsonMainConfig>> ConfigCache = new Dictionary<Type, Dictionary<int, IJsonMainConfig>>();

        public ConfigDatabase()
        {
        }
        
        public void LoadFromJson()
        {
            var configs = new Dictionary<Type, string>
            {
                { typeof(MatchConfig), "match.json" },
                { typeof(ModifierConfig), "modifier.json" },
                { typeof(BuffConfig), "buff.json" },
                { typeof(CardConfig), "card.json" },
            };

            foreach (var config in configs)
            {
                var cache = new Dictionary<int, IJsonMainConfig>();
                ConfigCache.Add(config.Key, cache);
                LoadConfigTable(config.Value, config.Key, cache);
            }
        }

        public T[] GetDataList<T>() where T : IJsonMainConfig
        {
            var type = typeof(T);
            if (ConfigCache.TryGetValue(type, out var cache))
            {
                return cache.Values.Cast<T>().ToArray();
            }

            Log.Info($"error config type : {type}");
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
                    Log.Info($"error {typeof(T).Name} config id : {id}");
                }
            }
            else
            {
                Log.Info($"error config type : {type}");
            }

            return default;
        }

        private void LoadConfigTable(string jsonFileName, Type type, Dictionary<int, IJsonMainConfig> cache)
        {
            LiteRuntime.Get<AssetSystem>().LoadAsset<TextAsset>(GetJsonFullPath(jsonFileName), (asset) =>
            {
                var data =  JsonUtils.DecodeArray(asset.text, type).Cast<IJsonMainConfig>().ToArray();
                if (data == null)
                {
                    Log.Info($"read json file error : {jsonFileName}");
                    return;
                }
            
                Log.Info($"load config : {jsonFileName}");
                foreach (var item in data)
                {
                    cache.Add(item.ID, item);
                }
            });
        }

        private string GetJsonFullPath(string jsonFileName)
        {
            return $"CardGame/Json/{jsonFileName}";
        }
    }
}