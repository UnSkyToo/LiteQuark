namespace InfiniteGame
{
    public sealed class Skill2 : SkillBase
    {
        private IBulletEmitter Emitter_;
        
        public Skill2()
        {
            Emitter_ = new BulletCircleEmitter();
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