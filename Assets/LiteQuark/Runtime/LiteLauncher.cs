using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [DefaultExecutionOrder(9999)]
    public class LiteLauncher : MonoBehaviour
    {
        [SerializeField] public LiteSetting DebugSetting = new();
        [SerializeField] public LiteSetting ReleaseSetting = new();
        [SerializeField] private bool _simulateReleaseBuild;

        private const int MaxConsecutiveErrors = 30;
        private int _consecutiveUpdateErrors;
        private int _consecutiveLateUpdateErrors;
        private LiteSetting _setting;

        private void Awake()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _setting = _simulateReleaseBuild ? ReleaseSetting : DebugSetting;
#else
            _setting = ReleaseSetting;
#endif

            try
            {
                DontDestroyOnLoad(this);
                LiteRuntime.Instance.Startup(this, _setting);
            }
            catch (Exception ex)
            {
                if (_setting.Common.ThrowException)
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
                if (_setting.Common.ThrowException)
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
                if (_setting.Common.ThrowException)
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
