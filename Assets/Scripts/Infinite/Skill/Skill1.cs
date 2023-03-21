using LiteQuark.Runtime;

namespace InfiniteGame
{
    public sealed class Skill1 : SkillBase
    {
        private float Interval_;
        private float Time_;
        
        public Skill1()
        {
        }
        
        public override void Tick(float deltaTime)
        {
            Time_ += deltaTime;
            if (Time_ >= GetAttackInterval())
            {
                Time_ = 0;
                FireBullet();
            }
        }

        private void FireBullet()
        {
            LiteRuntime.Get<TimerSystem>().AddTimer(GetBulletInterval(), () =>
            {
                var target = BattleManager.Instance.FindEnemy();
                if (target == null)
                {
                    return;
                }
                
                BattleManager.Instance.CreateBulletTrack(GetPlayer().GetPosition(), target.GetPosition(), 20);
            }, GetBulletCount());
        }

        private Player GetPlayer()
        {
            return BattleManager.Instance.GetPlayer();
        }

        private float GetAttackInterval()
        {
            return 1.0f - (Level - 1) * 0.1f;
        }

        private float GetBulletInterval()
        {
            return 0.083f;
        }

        private int GetBulletCount()
        {
            return Level;
        }
    }
}