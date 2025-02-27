using System;
using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    [LiteLabel("转移状态")]
    [LitePriority(uint.MaxValue - 1)]
    [Serializable]
    public sealed class LiteTransferEvent : ILiteEvent
    {
        [LiteProperty("目标", LitePropertyType.CustomPopupList)]
        [LiteCustomPopupList(typeof(LiteUnitBinderDataForEditor), nameof(LiteUnitBinderDataForEditor.GetCurrentUnitTimelinePathList))]
        public string StateName;

        [LiteProperty("条件列表", LitePropertyType.OptionalTypeList)]
        [LiteOptionalTypeList(typeof(ILiteCondition), "Condition", typeof(LiteFalseCondition))]
        [SerializeReference]
        [HideInInspector]
        public List<ILiteCondition> ConditionList = new List<ILiteCondition>();

        public bool HasData => true;
        
        public void Enter(LiteState state)
        {
        }

        public LiteEventSignal Tick(LiteState state, float deltaTime)
        {
            if (CheckConditionList(state))
            {
                state.Machine.SetNextState(StateName);
                return LiteEventSignal.Break;
            }

            return LiteEventSignal.Continue;
        }

        public void Leave(LiteState state)
        {
        }

        private bool CheckConditionList(LiteState state)
        {
            foreach (var condition in ConditionList)
            {
                if (!condition.Check(state))
                {
                    return false;
                }
            }

            return true;
        }

        public ILiteEvent Clone()
        {
            var evt = new LiteTransferEvent();
            evt.StateName = StateName;
            evt.ConditionList = new List<ILiteCondition>();
            foreach (var condition in ConditionList)
            {
                evt.ConditionList.Add(condition.Clone());
            }
            return evt;
        }
    }
}