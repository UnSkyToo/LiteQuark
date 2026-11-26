using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class DataSystem : ISystem, ISystemSettingProvider<DataSystemSetting>
    {
        public DataSystemSetting Setting { get; set; }

        private IDataProvider _provider;
        private bool _isClosing;
        private int _pendingOperations;

        public DataSystem()
        {
        }

        public async UniTask<bool> Initialize(CancellationToken ct = default)
        {
            switch (Setting.ProviderMode)
            {
                case DataProviderMode.PlayerPrefs:
                    _provider = new PlayerPrefsProvider(Setting.EnableEncryption, Setting.EncryptionKey);
                    break;

                case DataProviderMode.JsonFile:
                    _provider = new JsonFileProvider(Setting.EnableEncryption, Setting.EncryptionKey);
                    break;

                case DataProviderMode.BinaryFile:
                    _provider = new BinaryProvider(Setting.EnableEncryption, Setting.EncryptionKey);
                    break;

                case DataProviderMode.Custom:
                    _provider = TypeUtils.CreateEntryInstance(Setting.CustomProviderType);
                    break;

                default:
                    _provider = new PlayerPrefsProvider(Setting.EnableEncryption, Setting.EncryptionKey);
                    break;
            }

            return await _provider.Initialize(ct);
        }

        public async UniTask DisposeAsync()
        {
            _isClosing = true;
            while (Volatile.Read(ref _pendingOperations) > 0)
            {
                await UniTask.Yield();
            }

            var provider = _provider;
            _provider = null;
            provider?.Dispose();
        }

        #region Synchronous API

        public void Save<T>(string key, T data)
        {
            GetProvider().Save(key, data);
        }

        public T Load<T>(string key, T defaultValue = default)
        {
            return GetProvider().Load(key, defaultValue);
        }

        public bool Has(string key)
        {
            return GetProvider().Has(key);
        }

        public void Delete(string key)
        {
            GetProvider().Delete(key);
        }

        /// <summary>清空当前数据提供者的存储。</summary>
        /// <remarks>
        /// PlayerPrefs 模式会清空应用的全部 PlayerPrefs。JsonFile 和 BinaryFile 模式都会删除整个
        /// <c>SaveData</c> 目录，包括另一种文件模式的存档。自定义模式的范围由提供者决定。
        /// </remarks>
        public void DeleteAll()
        {
            GetProvider().DeleteAll();
        }

        #endregion

        #region Asynchronous API

        public async UniTask SaveAsync<T>(string key, T data)
        {
            var provider = GetProvider();
            Interlocked.Increment(ref _pendingOperations);
            try
            {
                await provider.SaveAsync(key, data);
            }
            finally
            {
                Interlocked.Decrement(ref _pendingOperations);
            }
        }

        public async UniTask<T> LoadAsync<T>(string key, T defaultValue = default)
        {
            var provider = GetProvider();
            Interlocked.Increment(ref _pendingOperations);
            try
            {
                return await provider.LoadAsync(key, defaultValue);
            }
            finally
            {
                Interlocked.Decrement(ref _pendingOperations);
            }
        }

        #endregion

        #region Provider Management

        public IDataProvider Provider => GetProvider();

        /// <summary>初始化新的数据提供者，成功后替换并释放旧提供者。</summary>
        /// <remarks>切换不会迁移已有数据。初始化失败时释放新提供者，继续使用旧提供者。</remarks>
        public async UniTask SwitchProvider(IDataProvider newProvider)
        {
            if (newProvider == null)
            {
                throw new ArgumentNullException(nameof(newProvider));
            }
            if (ReferenceEquals(GetProvider(), newProvider))
            {
                return;
            }

            Interlocked.Increment(ref _pendingOperations);
            try
            {
                try
                {
                    if (!await newProvider.Initialize())
                    {
                        throw new InvalidOperationException("Data provider initialization failed.");
                    }
                }
                catch
                {
                    await UniTask.SwitchToMainThread();
                    newProvider.Dispose();
                    throw;
                }

                await UniTask.SwitchToMainThread();
                var previous = _provider;
                _provider = newProvider;
                previous.Dispose();
            }
            finally
            {
                Interlocked.Decrement(ref _pendingOperations);
            }
        }

        #endregion

        private IDataProvider GetProvider()
        {
            if (_isClosing)
            {
                throw new ObjectDisposedException(nameof(DataSystem));
            }
            return _provider ?? throw new InvalidOperationException("Data provider is not ready.");
        }
    }
}
