using System;

namespace LiteBattle.Runtime
{
    [LiteLabel("True")]
    [LitePriority(uint.MaxValue - 1)]
    [Serializable]
    public class LiteTrueCondition : ILiteCondition
    {
        public bool HasData => false;
        
        public bool Check(LiteState state)
        {
            return true;
        }

        public ILiteCondition Clone()
        {
            return new LiteTrueCondition();
        }
    }
}