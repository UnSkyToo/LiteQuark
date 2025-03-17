using System;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    [LiteLabel("播放特效")]
    [Serializable]
    public sealed class LitePlayEffectEvent : ILiteEvent
    {
        [LiteProperty("特效文件", LitePropertyType.GameObject)]
        public string EffectPath;
        
        [LiteProperty("挂载位置", LitePropertyType.Vector3)]
        public Vector3 EffectPosition;
        
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
            var evt = new LitePlayEffectEvent();
            evt.EffectPath = EffectPath;
            evt.EffectPosition = EffectPosition;
            return evt;
        }
    }
}