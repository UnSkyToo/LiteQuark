using System.Collections.Generic;

namespace LiteCard.GamePlay
{
    public sealed class DamageSystem : Singleton<DamageSystem>
    {
        public void Handle(AgentBase caster, AgentBase target, int damageValue)
        {
            if (!target.IsAlive())
            {
                return;
            }

            var castCard = BattleContext.Current[BattleContextKey.CastCard] as CardBase;

            var data = new DamageData(caster, target, damageValue);
            BattleContext.Current[BattleContextKey.Damage] = data;

            BattleContext.Current[BattleContextKey.EventTargetList] = new List<AgentBase> { target };
            BuffSystem.Instance.TriggerBuff(BuffTriggerType.BeforeAttack, caster);
            BattleContext.Current[BattleContextKey.EventTargetList] = new List<AgentBase> { caster };
            BuffSystem.Instance.TriggerBuff(BuffTriggerType.BeforeBeAttack, target);
            
            Log.Info($"<{target.Name}> receive <{caster.Name}> damage : {data.Value}, source : {castCard?.GetName() ?? "unknown"}");

            var realDeductArmour = DeductArmour(data.Caster, data.Target, data.Value);
            if (realDeductArmour > 0)
            {
                BattleContext.Current[BattleContextKey.DeductArmour] = realDeductArmour;
                BattleContext.Current[BattleContextKey.EventTargetList] = new List<AgentBase> { caster };
                BuffSystem.Instance.TriggerBuff(BuffTriggerType.DeductArmour, target);
            }

            var realDeductHp = DeductHp(data.Caster, data.Target, data.Value - realDeductArmour);
            if (realDeductHp > 0)
            {
                BattleContext.Current[BattleContextKey.DeductHp] = realDeductHp;
                BattleContext.Current[BattleContextKey.EventTargetList] = new List<AgentBase> { caster };
                BuffSystem.Instance.TriggerBuff(BuffTriggerType.DeductHp, target);
            }
            
            BattleContext.Current[BattleContextKey.EventTargetList] = new List<AgentBase> { target };
            BuffSystem.Instance.TriggerBuff(BuffTriggerType.AfterAttack, caster);
            BattleContext.Current[BattleContextKey.EventTargetList] = new List<AgentBase> { caster };
            BuffSystem.Instance.TriggerBuff(BuffTriggerType.AfterBeAttack, target);

            if (!data.Target.IsAlive())
            {
                Log.Info($"<{data.Target.Name}> is dead");
                BattleContext.Current[BattleContextKey.EventTargetList] = new List<AgentBase> { target };
                BuffSystem.Instance.TriggerBuff(BuffTriggerType.KillMonster, caster);
            }
        }

        private int DeductHp(AgentBase caster, AgentBase target, int value)
        {
            if (value <= 0)
            {
                return 0;
            }

            target.ChangeAttr(AgentAttrType.CurHp, -value, 0);
            
            Log.Info($"{target.Name} hp {target.CurHp + value} -> {target.CurHp}, caster : {caster.Name}");
            return value;
        }

        private int DeductArmour(AgentBase caster, AgentBase target, int value)
        {
            if (target.Armour <= 0 || value <= 0)
            {
                return 0;
            }
            
            var costValue = 0;
            
            if (target.Armour < value)
            {
                costValue = target.Armour;
                ArmourSystem.Instance.Handle(caster, target, -target.Armour);
            }
            else
            {
                costValue = value;
                ArmourSystem.Instance.Handle(caster, target, -value);
            }
            
            return costValue;
        }
    }
}