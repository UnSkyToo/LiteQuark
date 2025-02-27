using System;
using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    [LiteLabel("监测按键")]
    [Serializable]
    public sealed class LiteInputKeyEvent : ILiteEvent
    {
        [LiteProperty("监测按键", LitePropertyType.CustomPopupList)]
        [LiteCustomPopupList(typeof(LiteConst.KeyName), nameof(LiteConst.KeyName.GetKeyList))]
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
            var inputKey = state.Unit.GetContext<string>(CheckType.ToContextKey(), LiteConst.ContextValue.None);
            if (inputKey == CheckKeyName)
            {
                state.Unit.SetContext(SetContextKey, SetContextValue);
            }
            else
            {
                state.Unit.SetContext(SetContextKey, LiteConst.ContextValue.None);
            }

            return LiteEventSignal.Continue;
        }

        public void Leave(LiteState state)
        {
            state.Unit.SetContext(SetContextKey, LiteConst.ContextValue.None);
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