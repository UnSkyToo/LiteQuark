using System;

namespace LiteBattle.Runtime
{   
    [LiteLabel("CheckContext")]
    [Serializable]
    public sealed class LiteCheckContextCondition : ILiteCondition
    {
        [LiteProperty("变量名", LitePropertyType.String)]
        public string Key = string.Empty;
        
        [LiteProperty("比较函数", LitePropertyType.Enum)]
        public LiteCompareKind CompareKind = LiteCompareKind.Equal;

        [LiteProperty("值", LitePropertyType.String)]
        public string Value = string.Empty;

        public bool HasData => true;
        
        public bool Check(LiteState state)
        {
            var contextValue = state.Agent.GetContext(Key, LiteConst.ContextValue.None);
            if (CompareKind == LiteCompareKind.NotEqual)
            {
                return contextValue != Value;
            }
            else
            {
                return contextValue == Value;
            }
        }

        public ILiteCondition Clone()
        {
            var cond = new LiteCheckContextCondition();
            cond.Key = Key;
            cond.CompareKind = CompareKind;
            cond.Value = Value;
            return cond;
        }
    }
}