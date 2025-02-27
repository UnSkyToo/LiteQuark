using System;
using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{   
    [LiteLabel("CheckTag")]
    [Serializable]
    public sealed class LiteCheckTagCondition : ILiteCondition
    {
        [LiteProperty("Tag", LitePropertyType.Enum)]
        public LiteTag Tag = LiteTag.None;
        
        [LiteProperty("值", LitePropertyType.Bool)]
        public bool Value = true;

        public bool HasData => true;
        
        public bool Check(LiteState state)
        {
            var tagValue = state.Unit.GetTag(Tag);
            return Value == tagValue;
        }

        public ILiteCondition Clone()
        {
            var cond = new LiteCheckTagCondition();
            cond.Tag = Tag;
            cond.Value = Value;
            return cond;
        }
    }
}