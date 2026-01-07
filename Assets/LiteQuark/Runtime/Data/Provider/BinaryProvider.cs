using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// 二进制文件数据提供者
    /// 适合存储大量数据，性能最好
    /// </summary>
    public sealed class BinaryProvider : IDataProvider
    {
        private readonly string _savePath;
        private readonly bool _enableEncryption;
        private readonly string _encryptionKey;

        public BinaryProvider(bool enableEncryption, string encryptionKey)
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

            // Serialize to JSON first (more portable than BinaryFormatter)
            var json = LitJson.JsonMapper.ToJson(data);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            if (_enableEncryption)
            {
                bytes = SecurityUtils.EncryptBytes(bytes, _encryptionKey);
            }

            File.WriteAllBytes(filePath, bytes);
        }

        public async UniTask SaveAsync<T>(string key, T data)
        {
            var filePath = GetFilePath(key);

            // Serialize to JSON first
            var json = LitJson.JsonMapper.ToJson(data);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            if (_enableEncryption)
            {
                bytes = SecurityUtils.EncryptBytes(bytes, _encryptionKey);
            }

            await File.WriteAllBytesAsync(filePath, bytes);
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
                var bytes = File.ReadAllBytes(filePath);

                if (_enableEncryption)
                {
                    bytes = SecurityUtils.DecryptBytes(bytes, _encryptionKey);
                }

                var json = System.Text.Encoding.UTF8.GetString(bytes);
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
                var bytes = await File.ReadAllBytesAsync(filePath);

                if (_enableEncryption)
                {
                    bytes = SecurityUtils.DecryptBytes(bytes, _encryptionKey);
                }

                var json = System.Text.Encoding.UTF8.GetString(bytes);
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
            return Path.Combine(_savePath, $"{key}.dat");
        }
    }
}
