using System;
using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    [LiteLabel("测试")]
    [Serializable]
    public sealed class LiteTestEvent : ILiteEvent
    {
        [LiteProperty("Go", LitePropertyType.GameObject)]
        public string Val = string.Empty;
        
        public bool HasData => true;
        
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
            return new LiteTestEvent();
        }
    }
}