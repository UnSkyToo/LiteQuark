using System;
using System.Diagnostics;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// 资源加载类型
    /// </summary>
    internal enum AssetLoadEventType
    {
        /// <summary>
        /// 完整的加载请求
        /// </summary>
        Session = 0,

        /// <summary>
        /// Bundle 加载
        /// </summary>
        Bundle = 1,

        /// <summary>
        /// Asset 从 Bundle 提取
        /// </summary>
        Asset = 2,

        /// <summary>
        /// Scene 加载
        /// </summary>
        Scene = 3,
    }

    /// <summary>
    /// 资源加载来源
    /// </summary>
    internal enum AssetLoadEventSource
    {
        Unknown = 0,
        Local = 1,
        Remote = 2,
        Cached = 3,
    }

    /// <summary>
    /// 加载事件时机
    /// </summary>
    internal enum AssetLoadEventTiming
    {
        Begin,
        End
    }

#if UNITY_EDITOR
    /// <summary>
    /// 资源加载事件数据
    /// 仅在 Editor 下可用
    /// </summary>
    internal readonly struct AssetLoadEventArgs
    {
        public AssetLoadEventTiming Timing { get; }
        public AssetLoadEventType TargetType { get; }
        public string AssetPath { get; }
        public string BundlePath { get; }
        public string[] Dependencies { get; }
        public bool IsSuccess { get; }
        public long FileSize { get; }
        public bool IsCached { get; }
        public bool IsRemote { get; }
        public string ErrorMessage { get; }

        public AssetLoadEventArgs(
            AssetLoadEventTiming timing,
            AssetLoadEventType targetType,
            string assetPath,
            string bundlePath,
            string[] dependencies = null,
            bool isSuccess = true,
            long fileSize = 0,
            bool isCached = false,
            bool isRemote = false,
            string errorMessage = null)
        {
            Timing = timing;
            TargetType = targetType;
            AssetPath = assetPath ?? string.Empty;
            BundlePath = bundlePath ?? string.Empty;
            Dependencies = dependencies ?? Array.Empty<string>();
            IsSuccess = isSuccess;
            FileSize = fileSize;
            IsCached = isCached;
            IsRemote = isRemote;
            ErrorMessage = errorMessage ?? string.Empty;
        }
    }
#endif

    /// <summary>
    /// 资源加载事件分发器
    /// 非 Editor 构建中，所有方法调用会被编译器完全剔除
    /// </summary>
    internal static class AssetLoadEventDispatcher
    {
#if UNITY_EDITOR
        public static event Action<AssetLoadEventArgs> OnLoadEvent;

        internal static void DispatchBegin(AssetLoadEventType targetType, string assetPath, string bundlePath,
            string[] dependencies = null, bool isCached = false)
        {
            var isRemote = LiteRuntime.Setting?.Asset?.BundleLocater == BundleLocaterMode.Remote;
            OnLoadEvent?.Invoke(new AssetLoadEventArgs(
                AssetLoadEventTiming.Begin,
                targetType,
                assetPath,
                bundlePath,
                dependencies,
                isCached: isCached,
                isRemote: isRemote));
        }

        internal static void DispatchEnd(AssetLoadEventType targetType, string assetPath, string bundlePath,
            bool isSuccess, long fileSize = 0, string errorMessage = null, bool isCached = false)
        {
            OnLoadEvent?.Invoke(new AssetLoadEventArgs(
                AssetLoadEventTiming.End,
                targetType,
                assetPath,
                bundlePath,
                isSuccess: isSuccess,
                fileSize: fileSize,
                errorMessage: errorMessage,
                isCached: isCached));
        }
#else
        [Conditional("UNITY_EDITOR")]
        internal static void DispatchBegin(AssetLoadEventType targetType, string assetPath, string bundlePath,
            string[] dependencies = null, bool isCached = false) { }

        [Conditional("UNITY_EDITOR")]
        internal static void DispatchEnd(AssetLoadEventType targetType, string assetPath, string bundlePath,
            bool isSuccess, long fileSize = 0, string errorMessage = null, bool isCached = false) { }
#endif
    }
}
