using System;

namespace LiteBattle.Runtime
{
    [LiteLabel("监测按键")]
    [Serializable]
    public sealed class LiteInputKeyEvent : ILiteEvent
    {
        [LiteProperty("监测按键", LitePropertyType.InputKeyList)]
        public string CheckKeyName;

        [LiteProperty("监测类型", LitePropertyType.Enum)]
        public LiteInputKeyType CheckType;
        
        [LiteProperty("设置属性Key", LitePropertyType.String)]
        public string SetContextKey;

        [LiteProperty("设置属性Value", LitePropertyType.String)]
        public string SetContextValue;

        public bool HasData => true;
        
        public void Enter(LiteState state)
        {
        }

        public LiteEventSignal Tick(LiteState state, float deltaTime)
        {
            var inputKey = state.Agent.GetContext<string>(CheckType.ToContextKey(), LiteConst.ContextValue.None);
            if (inputKey == CheckKeyName)
            {
                state.Agent.SetContext(SetContextKey, SetContextValue);
            }
            else
            {
                state.Agent.SetContext(SetContextKey, LiteConst.ContextValue.None);
            }

            return LiteEventSignal.Continue;
        }

        public void Leave(LiteState state)
        {
            state.Agent.SetContext(SetContextKey, LiteConst.ContextValue.None);
        }

        public ILiteEvent Clone()
        {
            var evt = new LiteInputKeyEvent();
            evt.CheckKeyName = CheckKeyName;
            evt.CheckType = CheckType;
            evt.SetContextKey = SetContextKey;
            evt.SetContextValue = SetContextValue;
            return evt;
        }
    }
}