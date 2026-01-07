using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// PlayerPrefs数据提供者
    /// 适合存储少量配置数据
    /// </summary>
    public sealed class PlayerPrefsProvider : IDataProvider
    {
        private readonly bool _enableEncryption;
        private readonly string _encryptionKey;

        public PlayerPrefsProvider(bool enableEncryption, string encryptionKey)
        {
            _enableEncryption = enableEncryption;
            _encryptionKey = encryptionKey;
        }

        public UniTask<bool> Initialize()
        {
            return UniTask.FromResult(true);
        }

        public void Save<T>(string key, T data)
        {
            var json = LitJson.JsonMapper.ToJson(data);

            if (_enableEncryption)
            {
                json = SecurityUtils.Encrypt(json, _encryptionKey);
            }

            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public UniTask SaveAsync<T>(string key, T data)
        {
            Save(key, data);
            return UniTask.CompletedTask;
        }

        public T Load<T>(string key, T defaultValue = default)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return defaultValue;
            }

            try
            {
                var json = PlayerPrefs.GetString(key);

                if (_enableEncryption)
                {
                    json = SecurityUtils.Decrypt(json, _encryptionKey);
                }

                return LitJson.JsonMapper.ToObject<T>(json);
            }
            catch (System.Exception ex)
            {
                LLog.Error($"Failed to load data for key '{key}': {ex.Message}");
                return defaultValue;
            }
        }

        public UniTask<T> LoadAsync<T>(string key, T defaultValue = default)
        {
            return UniTask.FromResult(Load(key, defaultValue));
        }

        public bool Has(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        public void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        public void Dispose()
        {
            PlayerPrefs.Save();
        }
    }
}
