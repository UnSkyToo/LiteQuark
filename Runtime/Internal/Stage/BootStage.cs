using UnityEngine;

namespace LiteQuark.Runtime
{
    internal sealed class BootStage : BaseStage
    {
        public BootStage()
        {
        }
        
        public override void Enter()
        {
            Application.targetFrameRate = LiteRuntime.Setting.Common.TargetFrameRate;
            Input.multiTouchEnabled = LiteRuntime.Setting.Common.MultiTouch;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Random.InitState((int) System.DateTime.Now.Ticks);
        }

        public override void Leave()
        {
        }

        public override StageCode Tick(float deltaTime)
        {
            return StageCode.Completed;
        }
    }
}