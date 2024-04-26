﻿using System;
using System.Collections.Generic;
using UnityEngine;

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
        [Header("日志设置")]
        [SerializeField] public LogSetting Log;
        [Header("界面设置")]
        [SerializeField] public UISetting UI;

        public LiteSetting()
        {
            LogicList = new List<LiteLogicEntryData>();

            Common = new CommonSetting();
            Asset = new AssetSetting();
            Log = new LogSetting();
            UI = new UISetting();
        }

        [Serializable]
        public class CommonSetting
        {
            [SerializeField] public int TargetFrameRate = 60;
            [SerializeField] public bool MultiTouch = false;

            [SerializeField] public bool AutoRestartInBackground = false;
            [SerializeField] public float BackgroundLimitTime = 90.0f;

            public CommonSetting()
            {
            }
        }

        [Serializable]
        public class AssetSetting
        {
            [SerializeField] public AssetLoaderMode AssetMode = AssetLoaderMode.Bundle;
            [SerializeField] public float BundleRetainMs = 5000f;

            public AssetSetting()
            {
            }
        }

        [Serializable]
        public class LogSetting
        {
            [SerializeField] public bool ReceiveLog = true;
            [SerializeField] public bool LogInfo = true;
            [SerializeField] public bool LogWarn = true;
            [SerializeField] public bool LogError = true;
            [SerializeField] public bool LogFatal = true;
            [SerializeField] public bool ShowLogViewer = true;

            public LogSetting()
            {
            }
        }

        [Serializable]
        public class UISetting
        {
            [SerializeField] public int ResolutionWidth = 1920;
            [SerializeField] public int ResolutionHeight = 1080;

            public UISetting()
            {
            }
        }
    }
}