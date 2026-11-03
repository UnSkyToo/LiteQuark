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

        public async UniTask<bool> Initialize()
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

            var result = await _provider.Initialize();
            return result;
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

        /// <summary>
        /// 保存数据（同步）
        /// </summary>
        public void Save<T>(string key, T data)
        {
            GetProvider().Save(key, data);
        }

        /// <summary>
        /// 加载数据（同步）
        /// </summary>
        public T Load<T>(string key, T defaultValue = default)
        {
            return GetProvider().Load(key, defaultValue);
        }

        /// <summary>
        /// 检查键是否存在
        /// </summary>
        public bool Has(string key)
        {
            return GetProvider().Has(key);
        }

        /// <summary>
        /// 删除指定键的数据
        /// </summary>
        public void Delete(string key)
        {
            GetProvider().Delete(key);
        }

        /// <summary>
        /// 删除所有数据
        /// </summary>
        public void DeleteAll()
        {
            GetProvider().DeleteAll();
        }

        #endregion

        #region Asynchronous API

        /// <summary>
        /// 保存数据（异步）
        /// </summary>
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

        /// <summary>
        /// 加载数据（异步）
        /// </summary>
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

        /// <summary>
        /// 获取当前使用的数据提供者
        /// </summary>
        public IDataProvider Provider => GetProvider();

        /// <summary>
        /// 切换数据提供者（高级功能）
        /// </summary>
        public async UniTask SwitchProvider(IDataProvider newProvider)
        {
            var previous = GetProvider();
            if (newProvider == null)
            {
                throw new ArgumentNullException(nameof(newProvider));
            }
            if (ReferenceEquals(previous, newProvider))
            {
                return;
            }

            Interlocked.Increment(ref _pendingOperations);
            try
            {
                var initialized = await newProvider.Initialize();
                await UniTask.SwitchToMainThread();
                if (!initialized)
                {
                    throw new InvalidOperationException("Data provider initialization failed.");
                }

                previous = _provider;
                _provider = newProvider;
                newProvider = null; // Ownership is published before old-provider disposal can invoke callbacks or throw.
                previous.Dispose();
            }
            catch
            {
                await UniTask.SwitchToMainThread();
                newProvider?.Dispose();
                throw;
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
