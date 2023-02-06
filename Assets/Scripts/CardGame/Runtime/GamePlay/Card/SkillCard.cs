using System.Collections.Generic;

namespace LiteCard.GamePlay
{
    public sealed class SkillCard : CardBase
    {
        public SkillCard(CardData data)
            : base(data)
        {
        }

        protected override bool OnCast(Player caster, AgentBase target)
        {
            var data = GetCastData();
            var targets = AgentMatcher.Match(caster, new List<AgentBase> { target }, data.Cfg.MatchID);
            ModifierSystem.Instance.Append(caster, targets, data);
            
            return true;
        }

        public override CardBase Clone()
        {
            return new SkillCard(Data_.Clone());
        }
    }
}