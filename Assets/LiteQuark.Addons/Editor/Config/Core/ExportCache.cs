using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using LitJson;

namespace LiteQuark.Editor
{
    internal sealed class ExportCache
    {
        private Dictionary<string, string> _hashMap;

        public ExportCache()
        {
            Load();
        }

        public bool IsFileChanged(string filePath)
        {
            var hash = ComputeFileHash(filePath);
            if (_hashMap.TryGetValue(filePath, out var cached))
            {
                return cached != hash;
            }

            return true;
        }

        public void UpdateHash(string filePath)
        {
            var hash = ComputeFileHash(filePath);
            _hashMap[filePath] = hash;
        }

        public void Save()
        {
            var json = JsonMapper.ToJson(_hashMap);
            ConfigToolCache.Cache = json;
        }

        public void RemoveHash(string filePath)
        {
            _hashMap.Remove(filePath);
        }

        public void Clear()
        {
            _hashMap.Clear();
            ConfigToolCache.Cache = string.Empty;
        }

        private void Load()
        {
            _hashMap = new Dictionary<string, string>();

            try
            {
                var json = ConfigToolCache.Cache;
                _hashMap = JsonMapper.ToObject<Dictionary<string, string>>(json);
                if (_hashMap == null)
                {
                    _hashMap = new Dictionary<string, string>();
                }
            }
            catch
            {
                _hashMap = new Dictionary<string, string>();
            }
        }

        private static string ComputeFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    var sb = new StringBuilder(hash.Length * 2);
                    foreach (var b in hash)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
        }
    }
}