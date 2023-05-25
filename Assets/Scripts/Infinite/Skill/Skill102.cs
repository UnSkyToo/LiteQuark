namespace InfiniteGame
{
    public sealed class Skill102 : SkillBase
    {
        public Skill102()
            : base("damage up")
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
            GetPlayer().DamageAdd++;
        }

        protected override void OnLevelUp()
        {
            GetPlayer().DamageAdd++;
        }
    }
}