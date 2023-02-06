using System;
using System.Collections.Generic;

namespace LiteCard.GamePlay
{
    public sealed class MatchFilterHandler : BattleHandlerBase<MatchFilterHandler>
    {
        private delegate List<AgentBase> Handler(AgentBase caster, List<AgentBase> targets, object[] paramList);
        private readonly Dictionary<MatchFilterType, Handler> HandlerList_;

        public MatchFilterHandler()
        {
            HandlerList_ = new Dictionary<MatchFilterType, Handler>
            {
                { MatchFilterType.None, null },
                { MatchFilterType.Buff, Handle_Buff },
                { MatchFilterType.CastCard, Handle_CastCard },
            };
        }

        public override EditorObjectArrayResult GetObjectArrayResult(object binder)
        {
            switch (binder)
            {
                case MatchFilterType.None:
                    return EditorObjectArrayResult.None;
                case MatchFilterType.Buff:
                    var buffResult = new EditorObjectArrayResult("BuffID", "比较函数", "比较值", typeof(int), typeof(CompareMethod), typeof(int));
                    buffResult.SetAttrs(0, new EditorDataPopupAttribute(EditorDataPopupType.Buff));
                    return buffResult;
                case MatchFilterType.CastCard:
                    var castCardResult = new EditorObjectArrayResult("卡牌ID", typeof(int));
                    castCardResult.SetAttrs(0, new EditorDataPopupAttribute(EditorDataPopupType.Card));
                    return castCardResult;
            }
            
            return EditorObjectArrayResult.None;
        }

        public List<AgentBase> Execute(AgentBase caster, List<AgentBase> targets, MatchConfig cfg)
        {
            if (cfg.FilterType == MatchFilterType.None || targets.Count == 0)
            {
                return targets;
            }

            var func = HandlerList_[cfg.FilterType];
            if (func == null)
            {
                return targets;
            }
            
            if (!CheckParam(cfg.FilterType, cfg.FilterParams))
            {
                return targets;
            }

            return func.Invoke(caster, targets, cfg.FilterParams);
        }
        
        private static readonly Dictionary<CompareMethod, Func<double, double, bool>> CompareMethodFunc_ = new Dictionary<CompareMethod, Func<double, double, bool>>
        {
            {CompareMethod.Equal, GameUtils.Approximately},
            {CompareMethod.Greater, (a, b) => a > b},
            {CompareMethod.GreaterEqual, (a, b) => a >= b},
            {CompareMethod.Less, (a, b) => a < b},
            {CompareMethod.LessEqual, (a, b) => a <= b},
        };

        private static List<AgentBase> Handle_Buff(AgentBase caster, List<AgentBase> targets, object[] paramList)
        {
            var buffID = (int)paramList[0];
            var methodFunc = (CompareMethod)paramList[1];
            var layer = (int)paramList[2];

            var func = CompareMethodFunc_[methodFunc];
            if (func == null)
            {
                return targets;
            }

            var list = new List<AgentBase>();
            foreach (var target in targets)
            {
                var buffLayer = target.GetBuffByID(buffID)?.Layer ?? 0;
                if (func.Invoke(buffLayer, layer))
                {
                    list.Add(target);
                }
            }

            return list;
        }

        private static List<AgentBase> Handle_CastCard(AgentBase caster, List<AgentBase> targets, object[] paramList)
        {
            if (BattleContext.Current[BattleContextKey.CastCard] is CardBase card)
            {
                var cardID = (int)paramList[0];
                if (card.GetCfg().ID == cardID)
                {
                    return targets;
                }
            }

            return new List<AgentBase>();
        }
    }
}