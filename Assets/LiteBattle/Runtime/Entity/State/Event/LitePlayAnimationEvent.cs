using System;

namespace LiteBattle.Runtime
{
    [LiteLabel("播放动画")]
    [Serializable]
    public sealed class LitePlayAnimationEvent : ILiteEvent
    {
        [LiteProperty("动画名字", LitePropertyType.AnimationNameList)]
        public string AnimationName;
        
        public bool HasData => true;
        
        public void Enter(LiteState state)
        {
            state.Agent.GetModule<LiteEntityBehaveModule>().PlayAnimation(AnimationName);
        }

        public LiteEventSignal Tick(LiteState state, float deltaTime)
        {
            return LiteEventSignal.Continue;
        }

        public void Leave(LiteState state)
        {
        }

        public ILiteEvent Clone()
        {
            var evt = new LitePlayAnimationEvent();
            evt.AnimationName = AnimationName;
            return evt;
        }
    }
}