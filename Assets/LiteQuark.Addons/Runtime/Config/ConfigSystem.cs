using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class ConfigSystem : ISystem, ISystemSettingProvider<ConfigSystemSetting>
    {
        public ConfigSystemSetting Setting { get; set; }

        private readonly Dictionary<Type, object> _tableMap = new();
        private readonly List<TableRegistration> _registrations = new();
        private readonly CancellationTokenSource _lifetimeCts = new();

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

        public UniTask DisposeAsync()
        {
            _lifetimeCts.Cancel();
            _tableMap.Clear();
            _registrations.Clear();

            return UniTask.CompletedTask;
        }

        public void RegisterTable<T>(string assetPath, Func<string, List<T>> parseFunc) where T : IConfigData
        {
            ThrowIfClosing();
            _registrations.Add(new TableRegistration(
                typeof(T),
                assetPath,
                json => new ConfigTable<T>(parseFunc(json))
            ));
        }

        public async UniTask LoadAll()
        {
            ThrowIfClosing();
            foreach (var reg in _registrations)
            {
                await LoadTable(reg);
            }
        }

        public async UniTask Load<T>() where T : IConfigData
        {
            ThrowIfClosing();
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
                using var handle = LiteRuntime.Asset.LoadAssetHandle<TextAsset>(reg.AssetPath, ct: _lifetimeCts.Token);
                await handle;
                var textAsset = handle.Result;
                if (textAsset == null)
                {
                    LLog.Error("ConfigSystem: failed to load asset {0}", reg.AssetPath);
                    return;
                }

                var table = reg.ParseFunc(textAsset.text);
                _tableMap[reg.DataType] = table;
            }
            catch (Exception ex)
            {
                _lifetimeCts.Token.ThrowIfCancellationRequested();
                LLog.Error("ConfigSystem: error loading {0}: {1}", reg.AssetPath, ex.Message);
            }
        }

        private void ThrowIfClosing()
        {
            if (_lifetimeCts.IsCancellationRequested)
            {
                throw new ObjectDisposedException(nameof(ConfigSystem));
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
