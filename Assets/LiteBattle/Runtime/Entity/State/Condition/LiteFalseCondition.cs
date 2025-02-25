using System;

namespace LiteBattle.Runtime
{
    [LiteLabel("False")]
    [LitePriority(uint.MaxValue)]
    [Serializable]
    public class LiteFalseCondition : ILiteCondition
    {
        public bool HasData => false;
        
        public bool Check(LiteState state)
        {
            return false;
        }

        public ILiteCondition Clone()
        {
            return new LiteFalseCondition();
        }
    }
}