using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public sealed class ArmourSystem : Singleton<ArmourSystem>
    {
        public void Handle(AgentBase caster, AgentBase target, int armourValue)
        {
            if (!target.IsAlive())
            {
                return;
            }

            var data = new ArmourData(caster, target, armourValue);
            BattleContext.Current[BattleContextKey.Armour] = data;
            
            BuffSystem.Instance.TriggerBuff(BuffTriggerType.BeforeChangeArmour, target);
            
            target.ChangeAttr(AgentAttrType.Armour, data.Value, 0);

            BuffSystem.Instance.TriggerBuff(BuffTriggerType.AfterChangeArmour, target);

            Log.Info($"{target.Name} armour {target.Armour - data.Value} -> {target.Armour}, source : {caster.Name}");
        }
    }
}