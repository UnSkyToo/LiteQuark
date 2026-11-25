using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class ConfigSystem : ISystem
    {
        private readonly Dictionary<Type, TableRegistration> _registrations = new();
        private readonly CancellationTokenSource _lifetimeCts = new();

        public UniTask<bool> Initialize(CancellationToken ct = default) => UniTask.FromResult(true);

        public UniTask DisposeAsync()
        {
            _lifetimeCts.Cancel();
            _registrations.Clear();

            return UniTask.CompletedTask;
        }

        public void RegisterTable<T>(string assetPath, Func<string, List<T>> parseFunc) where T : IConfigData
        {
            _registrations.Add(typeof(T), new TableRegistration(assetPath, asset => new ConfigTable<T>(parseFunc(asset.text))));
        }

        public void RegisterTable<T, TKey>(string assetPath, Func<TextAsset, List<T>> parseFunc, Func<T, TKey> keySelector)
        {
            _registrations.Add(typeof(T), new TableRegistration(assetPath,
                asset => new ConfigTable<T, TKey>(parseFunc(asset), keySelector)));
        }

        public async UniTask LoadAll(CancellationToken ct = default)
        {
            using var linked = ct.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetimeCts.Token) : null;
            var token = linked?.Token ?? _lifetimeCts.Token;
            token.ThrowIfCancellationRequested();

            var registrations = new List<TableRegistration>(_registrations.Values);
            var loads = new UniTask[registrations.Count];
            for (var i = 0; i < registrations.Count; i++)
            {
                loads[i] = LoadTable(registrations[i], token);
            }
            await UniTask.WhenAll(loads);
            token.ThrowIfCancellationRequested();
        }

        public async UniTask Load<T>(CancellationToken ct = default)
        {
            var type = typeof(T);
            if (!_registrations.TryGetValue(type, out var registration))
            {
                LLog.Warning("ConfigSystem: no registration found for {0}", type.Name);
                return;
            }

            using var linked = ct.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetimeCts.Token) : null;
            var token = linked?.Token ?? _lifetimeCts.Token;
            await LoadTable(registration, token);
            token.ThrowIfCancellationRequested();
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

        public ConfigTable<T> GetTable<T>() where T : IConfigData => GetLoadedTable<ConfigTable<T>>(typeof(T));

        public ConfigTable<T, TKey> GetTable<T, TKey>() => GetLoadedTable<ConfigTable<T, TKey>>(typeof(T));

        private TTable GetLoadedTable<TTable>(Type rowType) where TTable : class
        {
            if (_registrations.TryGetValue(rowType, out var registration) && registration.Table is TTable table)
            {
                return table;
            }

            LLog.Warning("ConfigSystem: table not loaded for {0} as {1}", rowType.Name, typeof(TTable));
            return null;
        }

        private async UniTask LoadTable(TableRegistration reg, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                using var handle = LiteRuntime.Asset.LoadAssetHandle<TextAsset>(reg.AssetPath, ct: ct);
                await handle;
                ct.ThrowIfCancellationRequested();
                var textAsset = handle.Result;
                if (textAsset == null)
                {
                    LLog.Error("ConfigSystem: failed to load asset {0}", reg.AssetPath);
                    return;
                }

                var table = reg.ParseFunc(textAsset);
                ct.ThrowIfCancellationRequested();
                reg.Table = table;
            }
            catch (Exception ex)
            {
                // Report cancellation after the caller has waited for every table to release its handle.
                if (!ct.IsCancellationRequested)
                {
                    LLog.Error("ConfigSystem: error loading {0}: {1}", reg.AssetPath, ex.Message);
                }
            }
        }

        private sealed class TableRegistration
        {
            public readonly string AssetPath;
            public readonly Func<TextAsset, object> ParseFunc;
            public object Table { get; set; }

            public TableRegistration(string assetPath, Func<TextAsset, object> parseFunc)
            {
                AssetPath = assetPath;
                ParseFunc = parseFunc;
            }
        }
    }
}
