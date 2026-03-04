using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class ConfigSystem : ISystem, ISystemSettingProvider<ConfigSystemSetting>
    {
        public ConfigSystemSetting Setting { get; set; }

        private readonly Dictionary<Type, object> _tableMap = new Dictionary<Type, object>();
        private readonly List<TableRegistration> _registrations = new List<TableRegistration>();

        public ConfigSystem()
        {
        }

        public async UniTask<bool> Initialize()
        {
            if (Setting.LoadAllOnInitialize)
            {
                await LoadAll();
            }

            return true;
        }

        public void Dispose()
        {
            _tableMap.Clear();
            _registrations.Clear();
        }

        public void RegisterTable<T>(string assetPath, Func<string, List<T>> parseFunc) where T : IConfigData
        {
            _registrations.Add(new TableRegistration(
                typeof(T),
                assetPath,
                (json) => new ConfigTable<T>(parseFunc(json))
            ));
        }

        public async UniTask LoadAll()
        {
            foreach (var reg in _registrations)
            {
                await LoadTable(reg);
            }
        }

        public async UniTask Load<T>() where T : IConfigData
        {
            var type = typeof(T);
            foreach (var reg in _registrations)
            {
                if (reg.DataType == type)
                {
                    await LoadTable(reg);
                    return;
                }
            }

            LLog.Warning("ConfigSystem: no registration found for {0}", type.Name);
        }

        public T GetData<T>(int id) where T : IConfigData
        {
            var table = GetTable<T>();
            if (table != null)
            {
                return table.Get(id);
            }

            return default;
        }

        public bool TryGetData<T>(int id, out T data) where T : IConfigData
        {
            var table = GetTable<T>();
            if (table != null)
            {
                return table.TryGet(id, out data);
            }

            data = default;
            return false;
        }

        public ConfigTable<T> GetTable<T>() where T : IConfigData
        {
            if (_tableMap.TryGetValue(typeof(T), out var obj))
            {
                return (ConfigTable<T>)obj;
            }

            LLog.Warning("ConfigSystem: table not loaded for {0}", typeof(T).Name);
            return null;
        }

        private async UniTask LoadTable(TableRegistration reg)
        {
            try
            {
                var textAsset = await LiteRuntime.Asset.LoadAssetAsync<TextAsset>(reg.AssetPath);
                if (textAsset == null)
                {
                    LLog.Error("ConfigSystem: failed to load asset {0}", reg.AssetPath);
                    return;
                }

                var table = reg.ParseFunc(textAsset.text);
                _tableMap[reg.DataType] = table;

                LiteRuntime.Asset.UnloadAsset(textAsset);
            }
            catch (Exception ex)
            {
                LLog.Error("ConfigSystem: error loading {0}: {1}", reg.AssetPath, ex.Message);
            }
        }

        private class TableRegistration
        {
            public readonly Type DataType;
            public readonly string AssetPath;
            public readonly Func<string, object> ParseFunc;

            public TableRegistration(Type dataType, string assetPath, Func<string, object> parseFunc)
            {
                DataType = dataType;
                AssetPath = assetPath;
                ParseFunc = parseFunc;
            }
        }
    }
}