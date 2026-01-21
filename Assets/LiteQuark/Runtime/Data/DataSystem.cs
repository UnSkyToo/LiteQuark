using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// 数据持久化系统
    /// </summary>
    public sealed class DataSystem : ISystem
    {
        private IDataProvider _provider;

        public DataSystem()
        {
        }

        public async UniTask<bool> Initialize()
        {
            var setting = LiteRuntime.Setting.Data;

            // Create provider based on mode
            switch (setting.ProviderMode)
            {
                case DataProviderMode.PlayerPrefs:
                    _provider = new PlayerPrefsProvider(setting.EnableEncryption, setting.EncryptionKey);
                    break;

                case DataProviderMode.JsonFile:
                    _provider = new JsonFileProvider(setting.EnableEncryption, setting.EncryptionKey);
                    break;

                case DataProviderMode.BinaryFile:
                    _provider = new BinaryProvider(setting.EnableEncryption, setting.EncryptionKey);
                    break;

                case DataProviderMode.Custom:
                    _provider = TypeUtils.CreateEntryInstance(setting.CustomProviderType);
                    break;
                
                default:
                    _provider = new PlayerPrefsProvider(setting.EnableEncryption, setting.EncryptionKey);
                    break;
            }

            var result = await _provider.Initialize();
            return result;
        }

        public void Dispose()
        {
            _provider?.Dispose();
            _provider = null;
        }

        #region Synchronous API

        /// <summary>
        /// 保存数据（同步）
        /// </summary>
        public void Save<T>(string key, T data)
        {
            _provider.Save(key, data);
        }

        /// <summary>
        /// 加载数据（同步）
        /// </summary>
        public T Load<T>(string key, T defaultValue = default)
        {
            return _provider.Load(key, defaultValue);
        }

        /// <summary>
        /// 检查键是否存在
        /// </summary>
        public bool Has(string key)
        {
            return _provider.Has(key);
        }

        /// <summary>
        /// 删除指定键的数据
        /// </summary>
        public void Delete(string key)
        {
            _provider.Delete(key);
        }

        /// <summary>
        /// 删除所有数据
        /// </summary>
        public void DeleteAll()
        {
            _provider.DeleteAll();
        }

        #endregion

        #region Asynchronous API

        /// <summary>
        /// 保存数据（异步）
        /// </summary>
        public async UniTask SaveAsync<T>(string key, T data)
        {
            await _provider.SaveAsync(key, data);
        }

        /// <summary>
        /// 加载数据（异步）
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string key, T defaultValue = default)
        {
            return await _provider.LoadAsync(key, defaultValue);
        }

        #endregion

        #region Provider Management

        /// <summary>
        /// 获取当前使用的数据提供者
        /// </summary>
        public IDataProvider Provider => _provider;

        /// <summary>
        /// 切换数据提供者（高级功能）
        /// </summary>
        public async UniTask SwitchProvider(IDataProvider newProvider)
        {
            _provider?.Dispose();
            _provider = newProvider;
            await _provider.Initialize();
        }

        #endregion
    }
}
