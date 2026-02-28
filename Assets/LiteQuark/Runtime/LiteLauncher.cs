using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [DefaultExecutionOrder(9999)]
    public class LiteLauncher : MonoBehaviour
    {
        [SerializeField] public LiteSetting Setting;
        
        private const int MaxConsecutiveErrors = 30;
        private int _consecutiveErrors = 0;
        
        private void Awake()
        {
            try
            {
                DontDestroyOnLoad(this);
                LiteRuntime.Instance.Startup(this);
            }
            catch (Exception ex)
            {
                if (Setting.Common.ThrowException)
                {
                    throw;
                }
                else
                {
                    LLog.Exception(ex);
                }
            }
        }

        private void Update()
        {
            try
            {
                LiteRuntime.Instance.Tick(Time.deltaTime);
                _consecutiveErrors = 0;
            }
            catch (Exception ex)
            {
                _consecutiveErrors++;
                if (_consecutiveErrors >= MaxConsecutiveErrors)
                {
                    LLog.Error("Too many consecutive errors, entering error state.");
                    LiteRuntime.Instance.EnterErrorState(FrameworkErrorCode.Unknown);
                    return;
                }
                
                if (Setting.Common.ThrowException)
                {
                    throw;
                }
                else
                {
                    LLog.Exception(ex);
                }
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
            else if (Input.GetKeyDown(KeyCode.F11))
            {
                LiteRuntime.Instance.EnterErrorState(FrameworkErrorCode.Unknown);
            }
            else if (Input.GetKeyDown(KeyCode.F12))
            {
                OnApplicationPause(!LiteRuntime.Instance.IsPause);
            }
#endif
        }
        
        private void OnApplicationQuit()
        {
            try
            {
                LiteRuntime.Instance.Shutdown();
            }
            catch (Exception ex)
            {
                if (Setting.Common.ThrowException)
                {
                    throw;
                }
                else
                {
                    LLog.Exception(ex);
                }
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