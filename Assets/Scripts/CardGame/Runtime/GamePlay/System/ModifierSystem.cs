using System.Collections.Generic;
using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public sealed class ModifierSystem : Singleton<ModifierSystem>
    {
        public ModifierSystem()
        {
        }
        
        public void Append(AgentBase caster, List<AgentBase> targets, ExecuteData data)
        {
            foreach (var modifier in data.Modifiers)
            {
                var executeTargetList = AgentMatcher.Match(caster, targets, modifier.Cfg.MatchID);
                ModifierHandler.Instance.Execute(caster, executeTargetList, modifier);
            }
        }
    }
}