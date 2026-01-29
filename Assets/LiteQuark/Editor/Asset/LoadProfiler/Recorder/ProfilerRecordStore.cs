using System;
using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEngine.Pool;

namespace LiteQuark.Editor.LoadProfiler
{
    /// <summary>
    /// Record 存储管理器
    /// 负责 Record 的存储、查询和生命周期管理
    /// </summary>
    internal sealed class ProfilerRecordStore
    {
        public const int DefaultMaxCount = 1000;
        public const int DefaultPoolSize = 100;

        public int MaxCount { get; set; } = DefaultMaxCount;

        public event Action<ProfilerRecord> OnRecordAdded;

        private readonly List<ProfilerRecord> _records = new();
        private readonly Dictionary<long, ProfilerRecord> _recordMap = new();

        // Pending 记录（等待 End 事件）
        private readonly Dictionary<string, ProfilerRecord> _pendingBundleRecords = new();
        private readonly Dictionary<string, ProfilerRecord> _pendingAssetRecords = new();
        private readonly Dictionary<string, ProfilerRecord> _pendingSceneRecords = new();

        private readonly ObjectPool<ProfilerRecord> _pool;
        private long _idCounter;

        public ProfilerRecordStore(int poolSize = DefaultPoolSize)
        {
            _pool = new ObjectPool<ProfilerRecord>(
                createFunc: () => new ProfilerRecord(),
                actionOnGet: r => r.Reset(),
                actionOnRelease: r => r.Reset(),
                maxSize: poolSize
            );
        }

        /// <summary>
        /// 创建 Bundle 的 Pending 记录
        /// </summary>
        public ProfilerRecord CreatePendingBundle(string bundlePath, AssetLoadEventSource source, string[] dependencies, long timestamp)
        {
            if (_pendingBundleRecords.ContainsKey(bundlePath)) return null;

            var record = _pool.Get();
            record.RecordId = ++_idCounter;
            record.BundlePath = bundlePath;
            record.Type = AssetLoadEventType.Bundle;
            record.Source = source;
            record.Dependencies = dependencies;
            record.StartTimestamp = timestamp;

            _pendingBundleRecords[bundlePath] = record;
            return record;
        }

        /// <summary>
        /// 完成 Bundle 记录
        /// </summary>
        public ProfilerRecord CompletePendingBundle(string bundlePath, bool isSuccess, long fileSize, string errorMessage, long timestamp)
        {
            if (!_pendingBundleRecords.TryGetValue(bundlePath, out var record)) return null;
            _pendingBundleRecords.Remove(bundlePath);

            record.EndTimestamp = timestamp;
            record.IsSuccess = isSuccess;
            record.Size = fileSize;
            record.ErrorMessage = errorMessage;

            Add(record);
            return record;
        }

        /// <summary>
        /// 创建 Asset/Scene 的 Pending 记录
        /// </summary>
        public ProfilerRecord CreatePendingAssetOrScene(AssetLoadEventType type, string assetPath, string bundlePath, AssetLoadEventSource source, long timestamp)
        {
            var pendingRecords = type == AssetLoadEventType.Asset ? _pendingAssetRecords : _pendingSceneRecords;
            var key = $"{bundlePath}|{assetPath}";

            if (pendingRecords.ContainsKey(key)) return null;

            var record = _pool.Get();
            record.RecordId = ++_idCounter;
            record.AssetPath = assetPath;
            record.BundlePath = bundlePath;
            record.Type = type;
            record.Source = source;
            record.StartTimestamp = timestamp;

            pendingRecords[key] = record;
            return record;
        }

        /// <summary>
        /// 完成 Asset/Scene 记录
        /// </summary>
        public ProfilerRecord CompletePendingAssetOrScene(AssetLoadEventType type, string assetPath, string bundlePath, bool isSuccess, string errorMessage, bool isCached, long timestamp)
        {
            var pendingRecords = type == AssetLoadEventType.Asset ? _pendingAssetRecords : _pendingSceneRecords;
            var key = $"{bundlePath}|{assetPath}";

            if (!pendingRecords.TryGetValue(key, out var record)) return null;
            pendingRecords.Remove(key);

            record.EndTimestamp = timestamp;
            record.IsSuccess = isSuccess;
            record.ErrorMessage = errorMessage;
            if (isCached)
            {
                record.Source = AssetLoadEventSource.Cached;
            }

            Add(record);
            return record;
        }

        /// <summary>
        /// 添加记录并触发事件
        /// </summary>
        private void Add(ProfilerRecord record)
        {
            _records.Add(record);
            _recordMap[record.RecordId] = record;

            // 清理旧记录
            while (_records.Count > MaxCount)
            {
                var oldRecord = _records[0];
                _records.RemoveAt(0);
                _recordMap.Remove(oldRecord.RecordId);
                _pool.Release(oldRecord);
            }

            OnRecordAdded?.Invoke(record);
        }

        /// <summary>
        /// 根据路径查询记录
        /// </summary>
        public List<ProfilerRecord> GetByPath(string path)
        {
            var result = new List<ProfilerRecord>();
            foreach (var record in _records)
            {
                if (record.AssetPath == path || record.BundlePath == path)
                {
                    result.Add(record);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取所有记录
        /// </summary>
        public List<ProfilerRecord> GetAll()
        {
            return new List<ProfilerRecord>(_records);
        }

        /// <summary>
        /// 获取最近的记录
        /// </summary>
        public List<ProfilerRecord> GetRecent(int count)
        {
            var result = new List<ProfilerRecord>();
            var startIndex = Math.Max(0, _records.Count - count);
            for (var i = startIndex; i < _records.Count; i++)
            {
                result.Add(_records[i]);
            }
            return result;
        }

        /// <summary>
        /// 获取耗时最长的记录
        /// </summary>
        public List<ProfilerRecord> GetHotspots(int count)
        {
            var sorted = new List<ProfilerRecord>(_records);
            sorted.Sort((a, b) => b.Duration.CompareTo(a.Duration));
            return sorted.GetRange(0, Math.Min(count, sorted.Count));
        }

        /// <summary>
        /// 计算统计信息
        /// </summary>
        public ProfilerStatistics GetStatistics()
        {
            var stats = new ProfilerStatistics();

            foreach (var record in _records)
            {
                stats.TotalRecordCount++;
                stats.TotalDuration += record.Duration;
                stats.TotalSize += record.Size;

                switch (record.Type)
                {
                    case AssetLoadEventType.Bundle:
                        stats.BundleCount++;
                        stats.BundleTotalDuration += record.Duration;
                        switch (record.Source)
                        {
                            case AssetLoadEventSource.Remote:
                                stats.RemoteBundleCount++;
                                break;
                            case AssetLoadEventSource.Local:
                                stats.LocalBundleCount++;
                                break;
                            case AssetLoadEventSource.Cached:
                                stats.CachedBundleCount++;
                                break;
                        }
                        break;
                    case AssetLoadEventType.Asset:
                        stats.AssetCount++;
                        stats.AssetTotalDuration += record.Duration;
                        break;
                    case AssetLoadEventType.Scene:
                        stats.SceneCount++;
                        stats.SceneTotalDuration += record.Duration;
                        break;
                }

                if (!record.IsSuccess) stats.FailedCount++;
            }

            if (stats.BundleCount > 0)
                stats.AverageBundleDuration = stats.BundleTotalDuration / stats.BundleCount;
            if (stats.AssetCount > 0)
                stats.AverageAssetDuration = stats.AssetTotalDuration / stats.AssetCount;
            if (stats.SceneCount > 0)
                stats.AverageSceneDuration = stats.SceneTotalDuration / stats.SceneCount;

            return stats;
        }

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            foreach (var record in _records) _pool.Release(record);
            _records.Clear();
            _recordMap.Clear();
            _pendingBundleRecords.Clear();
            _pendingAssetRecords.Clear();
            _pendingSceneRecords.Clear();
        }

        /// <summary>
        /// 清空 Pending 记录（禁用时调用）
        /// </summary>
        public void ClearPending()
        {
            _pendingBundleRecords.Clear();
            _pendingAssetRecords.Clear();
            _pendingSceneRecords.Clear();
        }
    }
}
