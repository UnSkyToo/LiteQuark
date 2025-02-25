using System;

namespace LiteBattle.Runtime
{
    [LiteLabel("Set Context")]
    [Serializable]
    public class LiteSetContextEvent : ILiteEvent
    {
        [LiteProperty("变量名", LitePropertyType.String)]
        public string Key = string.Empty;

        [LiteProperty("值", LitePropertyType.String)]
        public string Value = string.Empty;
        
        public bool HasData => true;
        
        public void Enter(LiteState state)
        {
        }

        public LiteEventSignal Tick(LiteState state, float deltaTime)
        {
            state.Agent.SetContext(Key, Value);
            return LiteEventSignal.Continue;
        }

        public void Leave(LiteState state)
        {
        }

        public ILiteEvent Clone()
        {
            var evt = new LiteSetContextEvent();
            evt.Key = Key;
            evt.Value = Value;
            return evt;
        }
    }
}