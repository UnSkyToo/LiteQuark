namespace LiteBattle.Runtime
{
    public sealed class LiteUnit : LiteEntity
    {
        public override string DebugName => $"Unit_{Tag}";

        private readonly LiteUnitConfig Config_;
        private LiteStateMachine StateMachine_;

        public LiteUnit(LiteUnitConfig config)
            : base()
        {
            Config_ = config;
        }

        public void Initialize(System.Action callback)
        {
            SetTag(LiteTag.CanMove, true);
            SetTag(LiteTag.CanJump, true);
            SetTag(LiteTag.Hit, false);
            
            LoadPrefab(Config_.PrefabPath, () =>
            {
                StateMachine_ = new LiteStateMachine(this, Config_.StateGroup);
                StateMachine_.SetNextState(Config_.EntryState);
                AttachModule<LiteEntityMovementModule>();
                AttachModule<LiteEntityBehaveModule>();
                AttachModule<LiteEntityDataModule>();
                AttachModule<LiteEntityHandleModule>();
                
                callback?.Invoke();
            });
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            StateMachine_?.Tick(deltaTime);
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