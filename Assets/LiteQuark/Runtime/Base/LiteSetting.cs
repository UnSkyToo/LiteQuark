using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public enum LiteBuildProfile
    {
        Development,
        Release,
    }

    public sealed class LiteSetting
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal const LiteBuildProfile DefaultEnvironment = LiteBuildProfile.Development;
#else
        internal const LiteBuildProfile DefaultEnvironment = LiteBuildProfile.Release;
#endif

        public string GameEntry;
        public List<LiteSystemEntry> SystemList { get; }
        public LiteBuildProfile Environment { get; }
        public CommonSetting Common { get; }
        public TaskSetting Task { get; }
        public AssetSetting Asset { get; }
        public ActionSetting Action { get; }
        public LogSetting Log { get; }

        public LiteSetting() : this(DefaultEnvironment)
        {
        }

        public LiteSetting(LiteBuildProfile environment) : this(environment, new LiteLauncherSettings())
        {
        }

        internal LiteSetting(LiteBuildProfile environment, LiteLauncherSettings copy)
        {
            Environment = environment;
            GameEntry = copy.GameEntry;
            SystemList = copy.SystemList;
            Common = copy.Common;
            Task = copy.Task;
            Asset = copy.Asset;
            Action = copy.Action;
            Log = copy.Log;

            Common.ApplyBuild(environment);
            Asset.ApplyBuild(environment);
            Log.ApplyBuild(environment);
        }

        [Serializable]
        public class CommonSetting
        {
            [SerializeField] public int TargetFrameRate = 60;
            [SerializeField] public bool MultiTouch = false;

            [Tooltip("回到前台时，若后台停留达到设定时长，则重启框架。")] [SerializeField]
            public bool AutoRestartInBackground = false;

            [Tooltip("触发重启所需的最短后台停留时间（秒）。")] [ConditionalShow(nameof(AutoRestartInBackground), true), SerializeField]
            public float BackgroundLimitTime = 90.0f;

            [Tooltip("当有异常时是否对外抛出，用于SDK崩溃收集")]
            [InspectorName("Throw Exception")] public BuildValue<bool> ThrowExceptionByBuild;
            public bool ThrowException { get => ThrowExceptionByBuild.Release; set => ThrowExceptionByBuild.Release = value; }
            internal void ApplyBuild(LiteBuildProfile profile) => ThrowExceptionByBuild.Apply(profile);

            [SerializeField, Range(0f, 10f)] public float TimeScale = 1.0f;

            public CommonSetting()
            {
            }
        }

        [Serializable]
        public class TaskSetting
        {
            [Tooltip("默认任务组的并发数量限制")] [Range(1, 50), SerializeField]
            public int ConcurrencyLimit = 20;

            public TaskSetting()
            {
            }
        }

        [Serializable]
        public class AssetSetting
        {
            [InspectorName("Bundle Mode")] public BuildValue<BundleLoadMode> BundleModeByBuild = new(BundleLoadMode.BuiltIn, BundleLoadMode.Simulate);
            [Tooltip("Remote 模式的 CDN 根地址，框架会追加平台和主版本目录。")]
            [InspectorName("Bundle Remote URI")] public BuildValue<string> BundleRemoteUriByBuild = new(string.Empty);

            public BundleLoadMode BundleMode { get => BundleModeByBuild.Release; set => BundleModeByBuild.Release = value; }
            public string BundleRemoteUri { get => BundleRemoteUriByBuild.Release; set => BundleRemoteUriByBuild.Release = value; }

            internal void ApplyBuild(LiteBuildProfile profile)
            {
                BundleModeByBuild.Apply(profile);
                BundleRemoteUriByBuild.Apply(profile);
            }

            [Tooltip("BundleLoader 同时提交的 Bundle 加载任务数量限制")]
            [Range(1, 30), SerializeField]
            public int ConcurrencyLimit = 5;

            [Tooltip("Asset 任务组的并发数量限制，包含版本包、Bundle、Asset 和 Scene 加载任务")]
            [Range(1, 50), SerializeField]
            public int TaskConcurrencyLimit = 20;

            [Tooltip("远程资源下载重试设置")] [SerializeField]
            public RetryParam BundleDownloadRetry;

            [Tooltip("是否屏蔽Unity Web Cache机制")] [SerializeField]
            public bool DisableUnityWebCache = false;

            [Tooltip("是否开启Bundle保留缓存，引用归零后延迟卸载")] [SerializeField]
            public bool EnableRetain = true;

            [Tooltip("开启缓存后，Bundle引用归零后的保留时间，单位（秒）")] [ConditionalShow(nameof(EnableRetain), true)] [SerializeField]
            public float BundleRetainTime = 300f; // 5 min

            [Tooltip("Editor下强制使用StreamingAssets资源，防止开发期间错误读取到PersistentData目录的缓存资源")] [SerializeField]
            public bool EditorForceStreamingAssets = true;

            public AssetSetting()
            {
                BundleDownloadRetry = new RetryParam();
            }
        }

        [Serializable]
        public class ActionSetting
        {
            [SerializeField] public bool SafetyMode = false;

            public ActionSetting()
            {
            }
        }

        [Serializable]
        public class LogSetting
        {
            [InspectorName("Levels")] public BuildValue<LogLevel> LevelsByBuild = new(LogLevel.Error | LogLevel.Fatal, LogLevel.All);
            [InspectorName("Show Log Viewer")] public BuildValue<bool> ShowLogViewerByBuild;

            public LogLevel Levels { get => LevelsByBuild.Release; set => LevelsByBuild.Release = value; }
            public bool ShowLogViewer { get => ShowLogViewerByBuild.Release; set => ShowLogViewerByBuild.Release = value; }

            internal void ApplyBuild(LiteBuildProfile profile)
            {
                LevelsByBuild.Apply(profile);
                ShowLogViewerByBuild.Apply(profile);
            }

            public LogSetting()
            {
            }
        }
    }
}
