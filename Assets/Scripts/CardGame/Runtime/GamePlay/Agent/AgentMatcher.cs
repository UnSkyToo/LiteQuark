using System.Collections.Generic;
using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public static class AgentMatcher
    {
        public static List<AgentBase> Match(AgentBase caster, List<AgentBase> targets, int matchID)
        {
            if (matchID == 0)
            {
                return targets;
            }

            var cfg = LiteRuntime.Get<ConfigSystem>().GetData<MatchConfig>(matchID);
            if (cfg == null)
            {
                return targets;
            }

            var result = MatchWithTarget(caster, targets, cfg.TargetType);
            result = MatchFilterHandler.Instance.Execute(caster, result, cfg);
            
            if (result.Count > cfg.TargetNum)
            {
                result.RemoveRange(cfg.TargetNum, result.Count - cfg.TargetNum);
            }
            return result;
        }

        private static List<AgentBase> MatchWithTarget(AgentBase caster, List<AgentBase> targets, MatchTargetType targetType)
        {
            if (targetType == MatchTargetType.None)
            {
                return targets;
            }

            switch (targetType)
            {
                case MatchTargetType.Player:
                    return new List<AgentBase> { AgentSystem.Instance.GetPlayer() };
                case MatchTargetType.Caster:
                    return new List<AgentBase> { caster };
                case MatchTargetType.Target:
                    return targets;
                case MatchTargetType.SelectTarget:
                    return new List<AgentBase> { BattleContext.Current[BattleContextKey.SelectTarget] as AgentBase };
                case MatchTargetType.Event:
                    return BattleContext.Current[BattleContextKey.EventTargetList] as List<AgentBase>;
                case MatchTargetType.Random:
                    return new List<AgentBase> { GameUtils.RandomMonster() };
                case MatchTargetType.AllMonster:
                    return new List<AgentBase>(AgentSystem.Instance.GetMonsterList());
                case MatchTargetType.All:
                    return new List<AgentBase>(AgentSystem.Instance.GetAllAgents());
            }

            return targets;
        }
    }
}