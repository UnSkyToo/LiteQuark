namespace InfiniteGame
{
    public sealed class Skill101 : SkillBase
    {
        public Skill101()
            : base("speed up")
        {
        }
        
        public override void Tick(float deltaTime)
        {
        }

        private Player GetPlayer()
        {
            return BattleManager.Instance.GetPlayer();
        }

        protected override void OnAttach()
        {
            GetPlayer().MoveSpeed += 0.5f;
        }

        protected override void OnLevelUp()
        {
            GetPlayer().MoveSpeed += 0.5f;
        }
    }
}