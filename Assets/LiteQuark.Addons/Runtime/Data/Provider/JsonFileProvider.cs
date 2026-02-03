using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// JSON文件数据提供者
    /// 适合存储结构化数据，可读性好
    /// </summary>
    public sealed class JsonFileProvider : IDataProvider
    {
        private readonly string _savePath;
        private readonly bool _enableEncryption;
        private readonly string _encryptionKey;

        public JsonFileProvider(bool enableEncryption, string encryptionKey)
        {
            _savePath = Application.persistentDataPath + "/SaveData";
            _enableEncryption = enableEncryption;
            _encryptionKey = encryptionKey;
        }

        public UniTask<bool> Initialize()
        {
            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }
            return UniTask.FromResult(true);
        }

        public void Save<T>(string key, T data)
        {
            var filePath = GetFilePath(key);
            var json = LitJson.JsonMapper.ToJson(data);

            if (_enableEncryption)
            {
                json = SecurityUtils.Encrypt(json, _encryptionKey);
            }

            File.WriteAllText(filePath, json);
        }

        public async UniTask SaveAsync<T>(string key, T data)
        {
            var filePath = GetFilePath(key);
            var json = LitJson.JsonMapper.ToJson(data);

            if (_enableEncryption)
            {
                json = SecurityUtils.Encrypt(json, _encryptionKey);
            }

            await File.WriteAllTextAsync(filePath, json);
        }

        public T Load<T>(string key, T defaultValue = default)
        {
            var filePath = GetFilePath(key);

            if (!File.Exists(filePath))
            {
                return defaultValue;
            }

            try
            {
                var json = File.ReadAllText(filePath);

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

        public async UniTask<T> LoadAsync<T>(string key, T defaultValue = default)
        {
            var filePath = GetFilePath(key);

            if (!File.Exists(filePath))
            {
                return defaultValue;
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);

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

        public bool Has(string key)
        {
            return File.Exists(GetFilePath(key));
        }

        public void Delete(string key)
        {
            var filePath = GetFilePath(key);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public void DeleteAll()
        {
            if (Directory.Exists(_savePath))
            {
                Directory.Delete(_savePath, true);
                Directory.CreateDirectory(_savePath);
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }

        private string GetFilePath(string key)
        {
            return Path.Combine(_savePath, $"{key}.json");
        }
    }
}
