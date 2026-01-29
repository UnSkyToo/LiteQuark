using System;
using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEngine.Pool;

namespace LiteQuark.Editor.LoadProfiler
{
    /// <summary>
    /// Session 存储管理器
    /// 负责 Session 的存储、查询和生命周期管理
    /// </summary>
    internal sealed class ProfilerSessionStore
    {
        public const int DefaultMaxCount = 100;
        public const int DefaultPoolSize = 50;

        public int MaxCount { get; set; } = DefaultMaxCount;

        public event Action<ProfilerSession> OnSessionCompleted;

        private readonly List<ProfilerSession> _sessions = new();
        private readonly Dictionary<long, ProfilerSession> _sessionMap = new();
        private readonly Dictionary<string, ProfilerSession> _activeSessionMap = new();
        private readonly ObjectPool<ProfilerSession> _pool;

        private long _idCounter;

        public ProfilerSessionStore(int poolSize = DefaultPoolSize)
        {
            _pool = new ObjectPool<ProfilerSession>(
                createFunc: () => new ProfilerSession(),
                actionOnGet: s => s.Reset(),
                actionOnRelease: s => s.Reset(),
                maxSize: poolSize
            );
        }

        /// <summary>
        /// 创建新的 Session
        /// </summary>
        public ProfilerSession Create(string requestPath, string mainBundlePath, long timestamp)
        {
            if (_activeSessionMap.ContainsKey(requestPath)) return null;

            var session = _pool.Get();
            session.SessionId = ++_idCounter;
            session.RequestPath = requestPath;
            session.MainBundlePath = mainBundlePath;
            session.RequestTimestamp = timestamp;
            session.DependencyTree = new ProfilerDependencyNode(requestPath, AssetLoadEventType.Session);

            _sessions.Add(session);
            _sessionMap[session.SessionId] = session;
            _activeSessionMap[requestPath] = session;

            // 清理旧会话（跳过仍在活跃状态的Session）
            while (_sessions.Count > MaxCount)
            {
                // 找到第一个已完成的Session进行清理
                var indexToRemove = -1;
                for (var i = 0; i < _sessions.Count; i++)
                {
                    if (_sessions[i].IsCompleted)
                    {
                        indexToRemove = i;
                        break;
                    }
                }

                // 如果没有已完成的Session可清理，停止清理（允许临时超出MaxCount）
                if (indexToRemove < 0) break;

                var oldSession = _sessions[indexToRemove];
                _sessions.RemoveAt(indexToRemove);
                _sessionMap.Remove(oldSession.SessionId);
                _activeSessionMap.Remove(oldSession.RequestPath);
                _pool.Release(oldSession);
            }

            return session;
        }

        /// <summary>
        /// 完成 Session
        /// </summary>
        public ProfilerSession Complete(string requestPath, bool isSuccess, long timestamp)
        {
            if (!_activeSessionMap.Remove(requestPath, out var session))
            {
                return null;
            }

            session.Complete(isSuccess, timestamp);
            ProfilerTreeBuilder.Build(session);
            OnSessionCompleted?.Invoke(session);
            return session;
        }

        /// <summary>
        /// 获取活跃的 Session
        /// </summary>
        public ProfilerSession GetActive(string requestPath)
        {
            return _activeSessionMap.GetValueOrDefault(requestPath);
        }

        /// <summary>
        /// 遍历所有活跃 Session
        /// </summary>
        public IEnumerable<ProfilerSession> GetActiveSessions()
        {
            return _activeSessionMap.Values;
        }

        /// <summary>
        /// 获取最近的 Session 列表
        /// </summary>
        public List<ProfilerSession> GetRecent(int count)
        {
            var result = new List<ProfilerSession>();
            var startIndex = Math.Max(0, _sessions.Count - count);
            for (var i = startIndex; i < _sessions.Count; i++)
            {
                result.Add(_sessions[i]);
            }
            return result;
        }

        /// <summary>
        /// 获取所有 Session
        /// </summary>
        public List<ProfilerSession> GetAll()
        {
            return new List<ProfilerSession>(_sessions);
        }

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            foreach (var session in _sessions) _pool.Release(session);
            _sessions.Clear();
            _sessionMap.Clear();
            _activeSessionMap.Clear();
        }

        /// <summary>
        /// 清空活跃 Session（禁用时调用）
        /// </summary>
        public void ClearActive()
        {
            _activeSessionMap.Clear();
        }
    }
}
