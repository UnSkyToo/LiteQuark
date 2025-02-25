using System;

namespace LiteBattle.Runtime
{
    [LiteLabel("Set Tag")]
    [Serializable]
    public class LiteSetTagEvent : ILiteEvent
    {
        [LiteProperty("Tag", LitePropertyType.Enum)]
        public LiteTag Tag = LiteTag.None;
        
        [LiteProperty("值", LitePropertyType.Bool)]
        public bool Value = true;

        [LiteProperty("是否还原", LitePropertyType.Bool)]
        public bool Revert = false;

        private bool OldValue_ = false;
        
        public bool HasData => true;
        
        public void Enter(LiteState state)
        {
            OldValue_ = state.Agent.GetTag(Tag);
        }

        public LiteEventSignal Tick(LiteState state, float deltaTime)
        {
            state.Agent.SetTag(Tag, Value);
            return LiteEventSignal.Continue;
        }

        public void Leave(LiteState state)
        {
            if (Revert)
            {
                state.Agent.SetTag(Tag, OldValue_);
            }
        }

        public ILiteEvent Clone()
        {
            var evt = new LiteSetTagEvent();
            evt.Tag = Tag;
            evt.Value = Value;
            return evt;
        }
    }
}