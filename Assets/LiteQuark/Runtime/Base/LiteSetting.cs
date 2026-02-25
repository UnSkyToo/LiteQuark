using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [Serializable]
    public sealed class LiteSetting
    {
        [Header("逻辑列表")]
        [SerializeField] public List<LiteTypeEntryData<ILogic>> LogicList;
        [Header("额外模块")]
        [SerializeField] public List<LiteTypeEntryData<ISystem>> SystemList;
        [Header("基础设置")]
        [SerializeField] public CommonSetting Common;
        [Header("任务设置")]
        [SerializeField] public TaskSetting Task;
        [Header("资源设置")]
        [SerializeField] public AssetSetting Asset;
        [Header("Action设置")]
        [SerializeField] public ActionSetting Action;
        [Header("日志设置")]
        [SerializeField] public LogSetting Log;
        
        [SerializeReference]
        public List<ISystemSetting> SystemSettings;

        public LiteSetting()
        {
            LogicList = new List<LiteTypeEntryData<ILogic>>();
            SystemList = new List<LiteTypeEntryData<ISystem>>();

            Common = new CommonSetting();
            Task = new TaskSetting();
            Asset = new AssetSetting();
            Action = new ActionSetting();
            Log = new LogSetting();

            SystemSettings = new List<ISystemSetting>();
        }

        [Serializable]
        public class CommonSetting
        {
            [SerializeField] public int TargetFrameRate = 60;
            [SerializeField] public bool MultiTouch = false;

            [Tooltip("是否允许后台固定时长自动重启")] [SerializeField]
            public bool AutoRestartInBackground = false;

            [Tooltip("开启后台重启后，设定重启时间，单位（秒)")] [ConditionalShow(nameof(AutoRestartInBackground), true), SerializeField]
            public float BackgroundLimitTime = 90.0f;

            [Tooltip("当有异常时是否对外抛出，用于SDK崩溃收集")] [SerializeField]
            public bool ThrowException = false;
            
            [SerializeField] public bool DebugMode = true;
            [SerializeField, Range(0f, 10f)] public float TimeScale = 1.0f;

            public CommonSetting()
            {
            }
        }
        
        [Serializable]
        public class TaskSetting
        {
            [Tooltip("并发任务数量限制")] [Range(1, 50), SerializeField]
            public int ConcurrencyLimit = 20;

            [Tooltip("忽略并发限制起始等级")]
            public TaskPriority IgnoreLimitPriority = TaskPriority.High;

            public TaskSetting()
            {
            }
        }
        
        [Serializable]
        public class AssetSetting
        {
            [Tooltip("资源模式，可选编辑器加载或者Bundle加载")] [SerializeField]
            public AssetProviderMode AssetMode = AssetProviderMode.Editor;
        
            [Tooltip("Bundle定位器，可选包内或者远端")] [ConditionalShow(nameof(AssetMode), (int)AssetProviderMode.Bundle), SerializeField]
            public BundleLocaterMode BundleLocater = BundleLocaterMode.BuiltIn;
        
            [Tooltip("远程资源根目录，根据主版本和平台动态分目录\n例如:https://localhost:8000/webgl/1.0/version_1.0.txt")] [ConditionalShow(nameof(AssetMode), (int)AssetProviderMode.Bundle, nameof(BundleLocater), (int)BundleLocaterMode.Remote), SerializeField]
            public string BundleRemoteUri = "https://localhost:8000/";
            
            [Tooltip("并发加载数量限制")] [Range(1, 30), SerializeField]
            public int ConcurrencyLimit = 5;
            
            [Tooltip("远程资源下载重试设置")] [ConditionalShow(nameof(AssetMode), (int)AssetProviderMode.Bundle, nameof(BundleLocater), (int)BundleLocaterMode.Remote), SerializeField]
            public RetryParam BundleDownloadRetry;
            
            [Tooltip("是否屏蔽Unity Web Cache机制")] [ConditionalShow(nameof(AssetMode), (int)AssetProviderMode.Bundle, nameof(BundleLocater), (int)BundleLocaterMode.Remote), SerializeField]
            public bool DisableUnityWebCache = false;
            
            [Tooltip("是否开启资源缓存模式，可以在释放资源后进行保留")] [ConditionalShow(nameof(AssetMode), (int)AssetProviderMode.Bundle), SerializeField]
            public bool EnableRetain = true;
        
            [Tooltip("开启缓存后，设定资源保留时间，单位（秒）")] [ConditionalShow(nameof(AssetMode), (int)AssetProviderMode.Bundle, nameof(EnableRetain), true)] [SerializeField]
            public float AssetRetainTime = 120; // 2 min
        
            [Tooltip("开启缓存后，设定Bundle保留时间，单位（秒）")] [ConditionalShow(nameof(AssetMode), (int)AssetProviderMode.Bundle, nameof(EnableRetain), true)] [SerializeField]
            public float BundleRetainTime = 300f; // 5 min
            
            [Tooltip("Editor下强制使用StreamingAssets资源，防止开发期间错误读取到PersistentData目录的缓存资源")] [ConditionalShow(nameof(AssetMode), (int)AssetProviderMode.Bundle), SerializeField]
            public bool EditorForceStreamingAssets = true;
            
            [Tooltip("编辑器模式下模拟异步加载的延迟")] [ConditionalShow(nameof(AssetMode), (int)AssetProviderMode.Editor), SerializeField]
            public bool SimulateAsyncDelayInEditor = true;
        
            [Tooltip("模拟异步加载的延迟时间范围，单位（帧)")] [ConditionalShow(nameof(AssetMode), (int)AssetProviderMode.Editor, nameof(SimulateAsyncDelayInEditor), true)] [SerializeField] [Range(1, 60)]
            public int AsyncDelayMinFrame = 1;
        
            [Tooltip("模拟异步加载的延迟时间范围，单位（帧）")] [ConditionalShow(nameof(AssetMode), (int)AssetProviderMode.Editor, nameof(SimulateAsyncDelayInEditor), true)] [SerializeField] [Range(1, 60)]
            public int AsyncDelayMaxFrame = 6;
        
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
            [SerializeField] public bool SimpleLog = true;
            [ConditionalShow(nameof(SimpleLog), false), SerializeField] public bool ReceiveLog = true;
            [ConditionalShow(nameof(SimpleLog), false, nameof(ReceiveLog), true), SerializeField] public bool LogInfo = true;
            [ConditionalShow(nameof(SimpleLog), false, nameof(ReceiveLog), true), SerializeField] public bool LogWarn = true;
            [ConditionalShow(nameof(SimpleLog), false, nameof(ReceiveLog), true), SerializeField] public bool LogError = true;
            [ConditionalShow(nameof(SimpleLog), false, nameof(ReceiveLog), true), SerializeField] public bool LogFatal = true;
            [ConditionalShow(nameof(SimpleLog), false, nameof(ReceiveLog), true), SerializeField] public bool ShowLogViewer = true;

            public LogSetting()
            {
            }
        }

        public ISystemSetting GetSetting(Type settingType)
        {
            var setting = LiteRuntime.Setting.SystemSettings?.FirstOrDefault(s => s != null && s.GetType() == settingType);
            if (setting == null)
            {
                LLog.Warning("Setting not found for {0}, using default", settingType.Name);
                setting = Activator.CreateInstance(settingType) as ISystemSetting;
            }
            return setting;
        }
        
        public T GetSetting<T>() where T : class, ISystemSetting, new()
        {
            return GetSetting(typeof(T)) as T;
        }
    }
}