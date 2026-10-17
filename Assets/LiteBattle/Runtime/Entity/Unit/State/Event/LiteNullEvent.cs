using System;
using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    [LiteLabel("无")]
    [LitePriority(uint.MaxValue)]
    [Serializable]
    public sealed class LiteNullEvent : ILiteEvent
    {
        public bool HasData => false;

        public void Enter(LiteState state)
        {
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
            return new LiteNullEvent();
        }
    }
}
