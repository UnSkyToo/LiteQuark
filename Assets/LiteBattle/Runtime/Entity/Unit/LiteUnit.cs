namespace LiteBattle.Runtime
{
    public sealed class LiteUnit : LiteEntity
    {
        public override string DebugName => $"Unit_{Tag}";

        private readonly LiteUnitConfig Config_;
        private readonly LiteStateMachine StateMachine_;

        public LiteUnit(LiteUnitConfig config)
            : base()
        {
            Config_ = config;
            
            SetTag(LiteTag.CanMove, true);
            SetTag(LiteTag.CanJump, true);
            SetTag(LiteTag.Hit, false);
                
            StateMachine_ = new LiteStateMachine(this, config.StateGroup);
            StateMachine_.SetNextState(config.EntryState);

            LoadPrefab(Config_.PrefabPath);
            
            AttachModule<LiteEntityMovementModule>();
            AttachModule<LiteEntityBehaveModule>();
            AttachModule<LiteEntityDataModule>();
            AttachModule<LiteEntityHandleModule>();
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            StateMachine_.Tick(deltaTime);
        }

        public LiteUnitConfig GetUnitConfig()
        {
            return Config_;
        }

        public LiteStateMachine GetStateMachine()
        {
            return StateMachine_;
        }
    }
}