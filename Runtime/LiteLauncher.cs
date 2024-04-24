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

        [Header("Log Setting")]
        [SerializeField] [HideInInspector] public bool ReceiveLog = true;
        [SerializeField] [HideInInspector] public bool LogInfo = true;
        [SerializeField] [HideInInspector] public bool LogWarn = true;
        [SerializeField] [HideInInspector] public bool LogError = true;
        [SerializeField] [HideInInspector] public bool LogFatal = true;
        [SerializeField] [HideInInspector] public bool ShowLogViewer = true;

        [Header("UI Setting")]
        [SerializeField] [HideInInspector] public int ResolutionWidth = 1920;
        [SerializeField] [HideInInspector] public int ResolutionHeight = 1080;
        
        private void Awake()
        {
            try
            {
                DontDestroyOnLoad(this);
                LiteRuntime.Instance.Startup(this);
            }
            catch (Exception ex)
            {
                LLog.Exception(ex);
            }
        }

        private void Update()
        {
            try
            {
                LiteRuntime.Instance.Tick(Time.deltaTime);
            }
            catch (Exception ex)
            {
                LLog.Exception(ex);
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

        private void OnApplicationQuit()
        {
            try
            {
                LiteRuntime.Instance.Shutdown();
            }
            catch (Exception ex)
            {
                LLog.Exception(ex);
            }
        }

        private void OnApplicationPause(bool pause)
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