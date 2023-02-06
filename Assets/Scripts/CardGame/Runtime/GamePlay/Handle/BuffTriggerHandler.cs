using System;
using System.Collections.Generic;

namespace LiteCard.GamePlay
{
    public sealed class BuffTriggerHandler : BattleHandlerBase<BuffTriggerHandler>
    {
        private readonly Dictionary<BuffTriggerType, Func<BuffData, object[], bool>> HandlerList_;

        public BuffTriggerHandler()
        {
            HandlerList_ = new Dictionary<BuffTriggerType, Func<BuffData, object[], bool>>
            {
                { BuffTriggerType.None, null },

                { BuffTriggerType.BeforeAttack, null },
                { BuffTriggerType.AfterAttack, null },
                { BuffTriggerType.BeforeBeAttack, null },
                { BuffTriggerType.AfterBeAttack, null },
                { BuffTriggerType.BeforeChangeArmour, null },
                { BuffTriggerType.AfterChangeArmour, null },

                { BuffTriggerType.DeductArmour, null },
                { BuffTriggerType.DeductHp, null },

                { BuffTriggerType.RoundBegin, null },
                { BuffTriggerType.RoundEnd, null },

                { BuffTriggerType.KillMonster, null },

                { BuffTriggerType.BeforeAttrChange, Handle_BeforeAttrChange },
                { BuffTriggerType.AfterAttrChange, Handle_AfterAttrChange },

                { BuffTriggerType.DrawCard, null },
                { BuffTriggerType.ConsumeCard, null },
                { BuffTriggerType.AfterCastCard, Handle_AfterCastCard },
            };
        }

        public override EditorObjectArrayResult GetObjectArrayResult(object binder)
        {
            switch (binder)
            {
                case BuffTriggerType.None:
                    return EditorObjectArrayResult.None;
                case BuffTriggerType.BeforeAttack:
                    return EditorObjectArrayResult.None;
                case BuffTriggerType.AfterAttack:
                    return EditorObjectArrayResult.None;
                case BuffTriggerType.BeforeBeAttack:
                    return EditorObjectArrayResult.None;
                case BuffTriggerType.AfterBeAttack:
                    return EditorObjectArrayResult.None;
                case BuffTriggerType.BeforeChangeArmour:
                    return EditorObjectArrayResult.None;
                case BuffTriggerType.AfterChangeArmour:
                    return EditorObjectArrayResult.None;
                case BuffTriggerType.DeductArmour:
                    return EditorObjectArrayResult.None;
                case BuffTriggerType.DeductHp:
                    return EditorObjectArrayResult.None;
                case BuffTriggerType.RoundBegin:
                    return EditorObjectArrayResult.None;
                case BuffTriggerType.RoundEnd:
                    return EditorObjectArrayResult.None;
                case BuffTriggerType.KillMonster:
                    return EditorObjectArrayResult.None;
                case BuffTriggerType.BeforeAttrChange:
                    return new EditorObjectArrayResult("属性类型", typeof(AgentAttrType));
                case BuffTriggerType.AfterAttrChange:
                    return new EditorObjectArrayResult("属性类型", typeof(AgentAttrType));
                case BuffTriggerType.DrawCard:
                    return EditorObjectArrayResult.None;
                case BuffTriggerType.ConsumeCard:
                    return EditorObjectArrayResult.None;
                case BuffTriggerType.AfterCastCard:
                    return new EditorObjectArrayResult("卡牌类型", typeof(CardType));
            }

            return EditorObjectArrayResult.None;
        }

        public void Execute(AgentBase caster, BuffData buff, object[] arguments)
        {
            var cfg = buff.Cfg;
            
            if (!CheckParam(cfg.TriggerType, cfg.TriggerParams))
            {
                return;
            }
            
            var func = HandlerList_[cfg.TriggerType];
            if (func?.Invoke(buff, arguments) ?? true)
            {
                BuffSystem.Instance.DoBuff(caster, buff);
            }
        }

        private bool Handle_BeforeAttrChange(BuffData buff, object[] arguments)
        {
            if (!GameUtils.CheckParam<AgentAttrType>(arguments))
            {
                return false;
            }

            return (AgentAttrType)arguments[0] == (AgentAttrType)buff.Cfg.TriggerParams[0];
        }

        private bool Handle_AfterAttrChange(BuffData buff, object[] arguments)
        {
            if (!GameUtils.CheckParam<AgentAttrType>(arguments))
            {
                return false;
            }
            
            return (AgentAttrType)arguments[0] == (AgentAttrType)buff.Cfg.TriggerParams[0];
        }

        private bool Handle_AfterCastCard(BuffData buff, object[] arguments)
        {
            if (!GameUtils.CheckParam<CardType>(arguments))
            {
                return false;
            }
            
            var needType = (CardType)buff.Cfg.TriggerParams[0];
            if (needType == CardType.None)
            {
                return true;
            }

            return (CardType)arguments[0] == needType;
        }
    }
}