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
        [Header("界面设置")]
        [SerializeField] public UISetting UI;
        [Header("调试设置")]
        [SerializeField] public DebugSetting Debug;

        public LiteSetting()
        {
            LogicList = new List<LiteLogicEntryData>();

            Common = new CommonSetting();
            Asset = new AssetSetting();
            Action = new ActionSetting();
            Log = new LogSetting();
            UI = new UISetting();
            Debug = new DebugSetting();
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

        [Serializable]
        public class DebugSetting
        {
            [SerializeField] public bool DebugMode;
            [SerializeField, Range(0f, 5f)] public float TimeScale = 1.0f;
            
            public DebugSetting()
            {
            }
        }
    }
}