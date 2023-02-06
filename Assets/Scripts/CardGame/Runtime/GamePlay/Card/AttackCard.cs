using System.Collections.Generic;

namespace LiteCard.GamePlay
{
    public sealed class AttackCard : CardBase
    {
        public AttackCard(CardData data)
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
            return new AttackCard(Data_.Clone());
        }
    }
}