using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [DefaultExecutionOrder(9999)]
    public class LiteLauncher : MonoBehaviour
    {
        public LiteLauncherSettings Settings = new();
#if UNITY_EDITOR
        public LiteBuildProfile EditorPreview = LiteBuildProfile.Development;
#endif

        private const int MaxConsecutiveErrors = 30;
        private int _consecutiveUpdateErrors;
        private int _consecutiveLateUpdateErrors;

        public LiteSetting CreateRuntimeSetting()
        {
#if UNITY_EDITOR
            var environment = EditorPreview;
#else
            const LiteBuildProfile environment = LiteSetting.DefaultEnvironment;
#endif
            return Settings.CreateRuntimeSetting(environment);
        }

        private void Awake()
        {
            try
            {
                DontDestroyOnLoad(this);
                LiteRuntime.Instance.Startup(this);
            }
            catch (Exception ex)
            {
                if (LiteRuntime.Setting?.Common.ThrowException == true)
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
            if (!RunFramePhase(static () => LiteRuntime.Instance.Tick(Time.deltaTime), ref _consecutiveUpdateErrors))
            {
                return;
            }

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.F5))
            {
                LiteRuntime.Instance.Restart();
            }
            else if (Input.GetKeyDown(KeyCode.F6))
            {
                LiteRuntime.Instance.Shutdown().Forget();
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

        private void LateUpdate()
        {
            RunFramePhase(static () => LiteRuntime.Instance.LateTick(), ref _consecutiveLateUpdateErrors);
        }

        private bool RunFramePhase(Action update, ref int consecutiveErrors)
        {
            try
            {
                update();
                consecutiveErrors = 0;
            }
            catch (Exception ex)
            {
                if (++consecutiveErrors >= MaxConsecutiveErrors)
                {
                    LLog.Error("Too many consecutive errors, entering error state.");
                    LiteRuntime.Instance.EnterErrorState(FrameworkErrorCode.Unknown);
                    return false;
                }
                if (LiteRuntime.Setting?.Common.ThrowException == true)
                {
                    throw;
                }
                LLog.Exception(ex);
            }
            return true;
        }

        private void OnApplicationQuit()
        {
            try
            {
                LiteRuntime.Instance.OnApplicationQuit();
            }
            catch (Exception ex)
            {
                if (LiteRuntime.Setting?.Common.ThrowException == true)
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
