using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiteQuark.Runtime;

namespace LiteQuark.Editor.LoadProfiler
{
    /// <summary>
    /// 资源加载分析器
    /// 通过订阅 AssetLoadEventDispatcher 事件来追踪加载过程
    /// 作为门面类协调 SessionStore 和 RecordStore
    /// </summary>
    internal sealed class ProfilerCore : Singleton<ProfilerCore>
    {
        public bool IsEnabled { get; private set; }

        public int MaxRecordCount
        {
            get => _recordStore.MaxCount;
            set => _recordStore.MaxCount = value;
        }

        public int MaxSessionCount
        {
            get => _sessionStore.MaxCount;
            set => _sessionStore.MaxCount = value;
        }

        public event Action<ProfilerRecord> OnRecordAdded
        {
            add => _recordStore.OnRecordAdded += value;
            remove => _recordStore.OnRecordAdded -= value;
        }

        public event Action<ProfilerSession> OnSessionCompleted
        {
            add => _sessionStore.OnSessionCompleted += value;
            remove => _sessionStore.OnSessionCompleted -= value;
        }

        private readonly ProfilerSessionStore _sessionStore = new();
        private readonly ProfilerRecordStore _recordStore = new();
        private readonly Stopwatch _stopwatch;
        private readonly object _lockObj = new();

        private ProfilerCore()
        {
            _stopwatch = Stopwatch.StartNew();
            SetEnabled(true);
        }

        public void SetEnabled(bool enabled)
        {
            if (IsEnabled == enabled) return;

            IsEnabled = enabled;
            if (enabled)
            {
                AssetLoadEventDispatcher.OnLoadEvent += HandleLoadEvent;
            }
            else
            {
                AssetLoadEventDispatcher.OnLoadEvent -= HandleLoadEvent;
                lock (_lockObj)
                {
                    _recordStore.ClearPending();
                    _sessionStore.ClearActive();
                }
            }
        }

        public long GetTimestamp() => _stopwatch.ElapsedMilliseconds;

        public void Clear()
        {
            lock (_lockObj)
            {
                _recordStore.Clear();
                _sessionStore.Clear();
            }
        }

        #region Event Handler

        private void HandleLoadEvent(AssetLoadEventArgs args)
        {
            if (!IsEnabled) return;

            lock (_lockObj)
            {
                switch (args.TargetType)
                {
                    case AssetLoadEventType.Session:
                        HandleSessionEvent(args);
                        break;
                    case AssetLoadEventType.Bundle:
                        HandleBundleEvent(args);
                        break;
                    case AssetLoadEventType.Asset:
                    case AssetLoadEventType.Scene:
                        HandleAssetOrSceneEvent(args);
                        break;
                }
            }
        }

        private void HandleSessionEvent(AssetLoadEventArgs args)
        {
            if (args.Timing == AssetLoadEventTiming.Begin)
            {
                _sessionStore.Create(args.AssetPath, args.BundlePath, GetTimestamp());
            }
            else
            {
                _sessionStore.Complete(args.AssetPath, args.IsSuccess, GetTimestamp());
            }
        }

        private void HandleBundleEvent(AssetLoadEventArgs args)
        {
            if (args.Timing == AssetLoadEventTiming.Begin)
            {
                // 注册 Bundle 到相关 Session
                RegisterBundleToSessions(args.BundlePath, args.Dependencies);

                var source = args.IsCached ? AssetLoadEventSource.Cached
                    : (args.IsRemote ? AssetLoadEventSource.Remote : AssetLoadEventSource.Local);
                _recordStore.CreatePendingBundle(args.BundlePath, source, args.Dependencies, GetTimestamp());
            }
            else
            {
                var record = _recordStore.CompletePendingBundle(args.BundlePath, args.IsSuccess, args.FileSize, args.ErrorMessage, GetTimestamp());
                if (record != null)
                {
                    TryAssociateRecordToSession(record);
                }
            }
        }

        private void HandleAssetOrSceneEvent(AssetLoadEventArgs args)
        {
            if (args.Timing == AssetLoadEventTiming.Begin)
            {
                var source = args.IsCached ? AssetLoadEventSource.Cached : AssetLoadEventSource.Local;
                _recordStore.CreatePendingAssetOrScene(args.TargetType, args.AssetPath, args.BundlePath, source, GetTimestamp());
            }
            else
            {
                var record = _recordStore.CompletePendingAssetOrScene(args.TargetType, args.AssetPath, args.BundlePath, args.IsSuccess, args.ErrorMessage, args.IsCached, GetTimestamp());
                if (record != null)
                {
                    TryAssociateRecordToSession(record);
                }
            }
        }

        private void RegisterBundleToSessions(string bundlePath, string[] dependencies)
        {
            foreach (var session in _sessionStore.GetActiveSessions())
            {
                if (session.MainBundlePath == bundlePath || session.RequiresBundle(bundlePath))
                {
                    session.RegisterBundle(bundlePath, dependencies);
                }
            }
        }

        private void TryAssociateRecordToSession(ProfilerRecord record)
        {
            if (record.Type == AssetLoadEventType.Asset || record.Type == AssetLoadEventType.Scene)
            {
                var session = _sessionStore.GetActive(record.AssetPath);
                session?.AddRecord(record);
                return;
            }

            if (record.Type == AssetLoadEventType.Bundle)
            {
                foreach (var session in _sessionStore.GetActiveSessions())
                {
                    if (session.RequiresBundle(record.BundlePath))
                    {
                        session.AddRecord(record);
                    }
                }
            }
        }

        #endregion

        #region Public Query API

        // Session 查询
        public ProfilerSession GetSessionByPath(string requestPath)
        {
            lock (_lockObj)
            {
                return _sessionStore.GetActive(requestPath);
            }
        }

        public List<ProfilerSession> GetRecentSessions(int count)
        {
            lock (_lockObj)
            {
                return _sessionStore.GetRecent(count);
            }
        }

        public List<ProfilerSession> GetAllSessions()
        {
            lock (_lockObj)
            {
                return _sessionStore.GetAll();
            }
        }

        // Record 查询
        public List<ProfilerRecord> GetRecordsByPath(string path)
        {
            lock (_lockObj)
            {
                return _recordStore.GetByPath(path);
            }
        }

        public List<ProfilerRecord> GetAllRecords()
        {
            lock (_lockObj)
            {
                return _recordStore.GetAll();
            }
        }

        public List<ProfilerRecord> GetRecentRecords(int count)
        {
            lock (_lockObj)
            {
                return _recordStore.GetRecent(count);
            }
        }

        // 统计
        public ProfilerStatistics GetStatistics()
        {
            lock (_lockObj)
            {
                return _recordStore.GetStatistics();
            }
        }

        public List<ProfilerRecord> GetHotspotRecords(int count)
        {
            lock (_lockObj)
            {
                return _recordStore.GetHotspots(count);
            }
        }

        #endregion
    }
}
