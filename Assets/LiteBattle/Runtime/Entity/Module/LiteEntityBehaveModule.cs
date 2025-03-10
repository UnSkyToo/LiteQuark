using System;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteEntityBehaveModule : LiteEntityModuleBase
    {
        private Animator Animator_;
        
        public LiteEntityBehaveModule(LiteEntity entity)
            : base(entity)
        {
            Animator_ = Entity.GetComponent<Animator>();
        }

        public override void Dispose()
        {
        }

        public override void Tick(float deltaTime)
        {
            UpdateHUD();
        }

        public void PlayAnimation(string animationName)
        {
            Animator_.CrossFade(animationName, 0, 0);
            // LiteLog.Info($"play : {animationName}");
        }

        private void UpdateHUD()
        {
            var curHp = Entity.GetModule<LiteEntityDataModule>().FinalValue(LiteEntityDataType.CurHp);
            var maxHp = Math.Max(1, Entity.GetModule<LiteEntityDataModule>().FinalValue(LiteEntityDataType.MaxHp));
            // var hpPercent = Math.Clamp(0d, 1d, curHp / maxHp);
        }
    }
}