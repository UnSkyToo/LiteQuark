using System;

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
            Entity.SetTag(LiteTag.Hit, true, 1);

            var damageValue = CalculateDamage(attacker, Entity);
            if (damageValue <= 0)
            {
                return;
            }

            Entity.GetModule<LiteEntityDataModule>().AddDelta(LiteEntityDataType.CurHp, -damageValue);
        }

        private double CalculateDamage(LiteEntity attacker, LiteEntity target)
        {
            var attackValue = attacker.GetModule<LiteEntityDataModule>().FinalValue(LiteEntityDataType.Atk);
            var defendValue = target.GetModule<LiteEntityDataModule>().FinalValue(LiteEntityDataType.Def);
            return Math.Max(attackValue - defendValue, 0);
        }
    }
}