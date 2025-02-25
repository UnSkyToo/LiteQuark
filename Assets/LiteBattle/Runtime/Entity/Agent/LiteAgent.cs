namespace LiteBattle.Runtime
{
    public sealed class LiteAgent : LiteEntity
    {
        public override string DebugName => $"Agent_{Tag}";

        private readonly LiteAgentConfig Config_;
        private readonly LiteStateMachine StateMachine_;

        public LiteAgent(LiteAgentConfig config)
            : base()
        {
            Config_ = config;
            
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

        public LiteAgentConfig GetAgentConfig()
        {
            return Config_;
        }

        public LiteStateMachine GetStateMachine()
        {
            return StateMachine_;
        }
    }
}