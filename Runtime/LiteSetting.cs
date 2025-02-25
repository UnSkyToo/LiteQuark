using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LiteQuark.Runtime
{
    [Serializable]
    public sealed class LiteSetting
    {
        [Header("逻辑列表")]
        [SerializeField] public List<LiteLogicEntryData> LogicList;
        [Header("基础设置")]
        [SerializeField] public CommonSetting Common;
        [Header("资源设置")]
        [SerializeField] public AssetSetting Asset;
        [Header("Action设置")]
        [SerializeField] public ActionSetting Action;
        [Header("日志设置")]
        [SerializeField] public LogSetting Log;
        [Header("调试设置")]
        [SerializeField] public DebugSetting Debug;
#if LITE_QUARK_ENABLE_UI
        [Header("界面设置")]
        [SerializeField] public UISetting UI;
#endif

        public LiteSetting()
        {
            LogicList = new List<LiteLogicEntryData>();

            Common = new CommonSetting();
            Asset = new AssetSetting();
            Action = new ActionSetting();
            Log = new LogSetting();
            Debug = new DebugSetting();
#if LITE_QUARK_ENABLE_UI
            UI = new UISetting();
#endif
        }

        [Serializable]
        public class CommonSetting
        {
            [SerializeField] public int TargetFrameRate = 60;
            [SerializeField] public bool MultiTouch = false;

            [SerializeField] public bool AutoRestartInBackground = false;
            [ConditionalHide(nameof(AutoRestartInBackground), true), SerializeField] public float BackgroundLimitTime = 90.0f;

            [SerializeField] public bool ThrowException = false;

            public CommonSetting()
            {
            }
        }

        [Serializable]
        public class AssetSetting
        {
            [SerializeField] public AssetLoaderMode AssetMode = AssetLoaderMode.Internal;
            [SerializeField] public bool EnableRetain = true;
            [ConditionalHide(nameof(EnableRetain), true), SerializeField] public float AssetRetainTime = 120; // 2 min
            [ConditionalHide(nameof(EnableRetain), true), SerializeField] public float BundleRetainTime = 300f; // 5 min
            [SerializeField] public bool EnableRemoteBundle = false;
            [ConditionalHide(nameof(EnableRemoteBundle), true), SerializeField] public string BundleRemoteUri = "https://localhost:8000/";
            
            public AssetSetting()
            {
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
            [SerializeField] public bool ReceiveLog = true;
            [ConditionalHide(nameof(ReceiveLog), true), SerializeField] public bool LogInfo = true;
            [ConditionalHide(nameof(ReceiveLog), true), SerializeField] public bool LogWarn = true;
            [ConditionalHide(nameof(ReceiveLog), true), SerializeField] public bool LogError = true;
            [ConditionalHide(nameof(ReceiveLog), true), SerializeField] public bool LogFatal = true;
            [ConditionalHide(nameof(ReceiveLog), true), SerializeField] public bool ShowLogViewer = true;

            public LogSetting()
            {
            }
        }

        [Serializable]
        public class DebugSetting
        {
            [SerializeField] public bool DebugMode;
            [SerializeField, Range(0f, 5f)] public float TimeScale = 1.0f;
            
            public DebugSetting()
            {
            }
        }
        
#if LITE_QUARK_ENABLE_UI
        [Serializable]
        public class UISetting
        {
            [SerializeField] public CanvasScaler.ScaleMode ScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            [SerializeField] public CanvasScaler.ScreenMatchMode MatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            [SerializeField] [Range(0, 1)] public float MatchValue = 0f;
            [SerializeField] public int ResolutionWidth = 1920;
            [SerializeField] public int ResolutionHeight = 1080;

            public UISetting()
            {
            }
        }
#endif
    }
}