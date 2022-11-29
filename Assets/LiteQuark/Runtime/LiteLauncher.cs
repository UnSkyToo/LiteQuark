using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public class LiteLauncher : MonoBehaviour
    {
        [SerializeField] [HideInInspector] public List<LiteLogicEntryData> LogicList;

        [SerializeField] [HideInInspector] public AssetLoaderMode AssetMode = AssetLoaderMode.Bundle;

        [Header("Base Setting")]
        [SerializeField] [HideInInspector] public int TargetFrameRate = 60;
        
        [SerializeField] [HideInInspector] public bool MultiTouch;

        [SerializeField] [HideInInspector] public bool AutoRestartInBackground;
        [SerializeField] [HideInInspector] public float BackgroundLimitTime = 90.0f;

        [SerializeField] [HideInInspector] public bool ReceiveLog = true;
        [SerializeField] [HideInInspector] public bool LogInfo = true;
        [SerializeField] [HideInInspector] public bool LogWarn = true;
        [SerializeField] [HideInInspector] public bool LogError = true;
        [SerializeField] [HideInInspector] public bool LogFatal = true;
        [SerializeField] [HideInInspector] public bool ShowLogViewer = true;

        void Awake()
        {
            try
            {
                DontDestroyOnLoad(this);
                LiteRuntime.Instance.Startup(this);
            }
            catch (Exception ex)
            {
                LLog.Error($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        void Update()
        {
            try
            {
                LiteRuntime.Instance.Tick(Time.deltaTime);
            }
            catch (Exception ex)
            {
                LLog.Error($"{ex.Message}\n{ex.StackTrace}");
            }

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.F5))
            {
                LiteRuntime.Instance.Restart();
            }
            else if (Input.GetKeyDown(KeyCode.F6))
            {
                LiteRuntime.Instance.Shutdown();
            }
            else if (Input.GetKeyDown(KeyCode.F12))
            {
                OnApplicationPause(!LiteRuntime.Instance.IsPause);
            }
#endif
        }

        private void OnGUI()
        {
            LiteRuntime.Instance.OnGUI();
        }

        void OnApplicationQuit()
        {
            try
            {
                LiteRuntime.Instance.Shutdown();
            }
            catch (Exception ex)
            {
                LLog.Error($"{ex.Message}\n{ex.StackTrace}");
            }
        }

        void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                LiteRuntime.Instance.OnEnterBackground();
            }
            else
            {
                LiteRuntime.Instance.OnEnterForeground();
            }
        }
    }
}