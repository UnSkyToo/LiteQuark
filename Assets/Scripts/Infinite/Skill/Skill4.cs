namespace InfiniteGame
{
    public sealed class Skill4 : SkillBase
    {
        private float Interval_;
        private float Time_;

        public Skill4()
            : base("flash")
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
            var target = BattleManager.Instance.FindEnemy(10);
             if (target == null)
             {
                 return;
             }
             
             BattleManager.Instance.CreateBulletWink(target.GetPosition(), 0.5f);
        }

        private Player GetPlayer()
        {
            return BattleManager.Instance.GetPlayer();
        }

        private float GetAttackInterval()
        {
            return 5.0f - (Level - 1) * 0.5f;
        }
    }
}