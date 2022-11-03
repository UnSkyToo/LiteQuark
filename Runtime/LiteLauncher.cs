using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public class LiteLauncher : MonoBehaviour
    {
        public string LogicClassName;
        public AssetLoaderMode AssetMode = AssetLoaderMode.Bundle;

        void Awake()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(LogicClassName))
                {
                    throw new Exception($"please select logic entry in <{nameof(LiteLauncher)}>");
                }

                var logicType = TypeHelper.GetTypeWithLogicClassName(LogicClassName);
                if (logicType == null)
                {
                    throw new Exception($"can't not find game logic class type : {LogicClassName}");
                }

                if (Activator.CreateInstance(logicType) is not IGameLogic logic)
                {
                    throw new Exception("Game Logic must not be null");
                }

                LiteRuntime.Instance.Startup(this, logic);
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