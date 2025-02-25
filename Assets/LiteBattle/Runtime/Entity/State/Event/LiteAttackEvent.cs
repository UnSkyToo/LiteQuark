using System;
using UnityEngine;

namespace LiteBattle.Runtime
{
    [LiteLabel("攻击")]
    [Serializable]
    public sealed class LiteAttackEvent : ILiteEvent
    {
        [LiteProperty("范围", LitePropertyType.OptionalType)]
        [LiteOptionalType(typeof(ILiteRange), "Range Data")]
        [SerializeReference]
        [HideInInspector]
        public ILiteRange Range = new LiteBoxRange();
        
        public bool HasData => true;
        
        public void Enter(LiteState state)
        {
            var targets = LiteCollisionMgr.Instance.CheckCollide(state.Agent, Range, state.Agent.Camp.Reverse());
            foreach (var target in targets)
            {
                target.GetModule<LiteEntityHandleModule>()?.HandleBeAttack(state.Agent);
            }
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
            var evt = new LiteAttackEvent();
            evt.Range = Range.Clone();
            return evt;
        }
    }
}