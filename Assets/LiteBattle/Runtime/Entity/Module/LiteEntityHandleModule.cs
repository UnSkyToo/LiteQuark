using System;
using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    public sealed class LiteEntityHandleModule : LiteEntityModuleBase
    {
        public LiteEntityHandleModule(LiteEntity entity)
            : base(entity)
        {
        }

        public override void Dispose()
        {
        }

        public override void Tick(float deltaTime)
        {
        }

        public void HandleBeAttack(LiteEntity attacker)
        {
            var damageValue = CalculateDamage(attacker, Entity);
            if (damageValue <= 0)
            {
                return;
            }

            Entity.GetModule<LiteEntityDataModule>().AddDelta(LiteEntityDataType.CurHp, -damageValue);
            LLog.Info($"{attacker.DebugName} 攻击 {Entity.DebugName}，造成 {damageValue} 点伤害");

            if (Entity.GetModule<LiteEntityDataModule>().FinalValue(LiteEntityDataType.CurHp) <= 0)
            {
                Entity.SetContext("player_state_dead", "dead");
                LLog.Info($"{Entity.DebugName} 死亡");
            }

            LiteRuntime.Get<UISystem>().OpenUI<UIFloatText>(UIConfigs.UIFloatText, Entity, $"-{damageValue}");
        }

        private double CalculateDamage(LiteEntity attacker, LiteEntity target)
        {
            var attackValue = attacker.GetModule<LiteEntityDataModule>().FinalValue(LiteEntityDataType.Atk);
            var defendValue = target.GetModule<LiteEntityDataModule>().FinalValue(LiteEntityDataType.Def);
            return Math.Max(attackValue - defendValue, 0);
        }
    }
}