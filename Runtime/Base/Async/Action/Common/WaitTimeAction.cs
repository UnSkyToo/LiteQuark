namespace LiteQuark.Runtime
{
    public class WaitTimeAction : BaseAction
    {
        public override string DebugName => $"<WaitTime>({WaitTime_})";

        private readonly float WaitTime_ = 0f;
        private float CurrentTime_ = 0f;
        
        public WaitTimeAction(float waitTime)
        {
            WaitTime_ = waitTime;
        }

        public override void Tick(float deltaTime)
        {
            CurrentTime_ -= deltaTime;
            if (CurrentTime_ <= 0f)
            {
                IsEnd = true;
            }
        }

        public override void Execute()
        {
            CurrentTime_ = WaitTime_;
            IsEnd = CurrentTime_ <= 0f;
        }
    }

    public class WaitUntilAction : BaseAction
    {
        public override string DebugName => $"<WaitUntil>({ConditionFunc_})";
        
        private readonly System.Func<bool> ConditionFunc_ = null;

        public WaitUntilAction(System.Func<bool> conditionFunc)
        {
            ConditionFunc_ = conditionFunc;
        }

        public override void Execute()
        {
            IsEnd = false;
        }

        public override void Tick(float deltaTime)
        {
            IsEnd = ConditionFunc_?.Invoke() ?? true;
        }
    }
}