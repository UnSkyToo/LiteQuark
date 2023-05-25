namespace InfiniteGame
{
    public sealed class Skill3 : SkillBase
    {
        private IBulletEmitter Emitter_;
        
        public Skill3()
            : base("domain")
        {
            Emitter_ = new BulletAreaEmitter();
        }
        
        public override void Tick(float deltaTime)
        {
            Emitter_.Move(GetPlayer().GetPosition());
        }

        private Player GetPlayer()
        {
            return BattleManager.Instance.GetPlayer();
        }

        protected override void OnAttach()
        {
            Emitter_.Fire(Level);
        }

        protected override void OnLevelUp()
        {
            Emitter_.Fire(Level);
        }
    }
}