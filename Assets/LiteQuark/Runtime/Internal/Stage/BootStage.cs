using UnityEngine;

namespace LiteQuark.Runtime
{
    internal sealed class BootStage : IStage
    {
        public BootStage()
        {
        }
        
        public void Enter()
        {
            Application.targetFrameRate = LiteRuntime.Setting.Common.TargetFrameRate;
            Input.multiTouchEnabled = LiteRuntime.Setting.Common.MultiTouch;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Random.InitState((int) System.DateTime.Now.Ticks);
        }

        public void Leave()
        {
        }

        public StageCode Tick(float deltaTime)
        {
            return StageCode.Completed;
        }
    }
}